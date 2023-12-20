using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Morphing
{
    /// <summary>
    /// GPUShapeOfModel is a Unity MonoBehaviour that utilizes GPU compute shaders
    /// to morph between different 3D models by manipulating their vertices.
    /// </summary>
    public class GPUShapeOfModel : MonoBehaviour
    {
        // Constants
        private const int MaxResolution = 1000;

        // Serialized Fields
        [SerializeField] private List<Mesh> meshes;           // List of 3D meshes to morph between.
        [SerializeField] private ComputeShader computeShader; // GPU compute shader for vertex manipulation.
        [SerializeField] private Material material;           // Material for rendering the morphed shape.
        [SerializeField] private Mesh mesh;                   // Base mesh for rendering instanced procedural geometry.

        // Parameters
        [SerializeField, Range(10, MaxResolution)]
        private int resolution = 10; // Resolution of the morphed shape.

        // Private Variables
        private float sqrtVertices;
        private bool isTransitioning;
        private float transitionDuration;
        private float vertexCount;

        // Shader Property IDs
        private static readonly int
            PositionsId = Shader.PropertyToID("positions"),
            TrianglesId = Shader.PropertyToID("triangle_buffer"),
            VerticesId = Shader.PropertyToID("vertex_buffer"),
            ResolutionId = Shader.PropertyToID("resolution"),
            TrianglesCountId = Shader.PropertyToID("triangles_count"),
            StepId = Shader.PropertyToID("_step"),
            ScaleId = Shader.PropertyToID("scale"),
            TransitionProgressId = Shader.PropertyToID("transition_progress"),
            PointsPerTriangleId = Shader.PropertyToID("points_per_triangle"),
            TotalCountId = Shader.PropertyToID("positions_count"),
            CalculationKernelIndex = 0,
            TransformKernelIndex = 1;

        // Compute Buffers
        private ComputeBuffer positionsBuffer;
        private ComputeBuffer totalArea;
        private ComputeBuffer triAngle;
        private ComputeBuffer vertexBuffer;
        private ComputeBuffer trianglesBuffer;

        // Other Variables
        private int totalPointsCount;
        private int currentMeshIndex = -1;
        private int targetMeshIndex = 33;
        private int groupX;
        private int groupY;
        private float step;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        private void OnEnable()
        {
            // Initialize with a random target mesh.
            targetMeshIndex = Random.Range(0, meshes.Count);
            CalculateCurrentMeshPositions(CalculationKernelIndex);
            InitTargetBuffersIndex();
        }

        /// <summary>
        /// Calculates the positions of the current target mesh using GPU compute shaders.
        /// </summary>
        /// <param name="kernelIndex">Index of the compute shader kernel to use.</param>
        private void CalculateCurrentMeshPositions(int kernelIndex)
        {
            var currentMesh = meshes[targetMeshIndex];
            
            // Create compute buffers for vertices and triangles.
            positionsBuffer = new ComputeBuffer(resolution * resolution, 3 * sizeof(float));
            vertexBuffer = new ComputeBuffer(currentMesh.vertexCount, 3 * sizeof(float));
            vertexBuffer.SetData(currentMesh.vertices);
            vertexCount = vertexBuffer.count;

            var triangles = currentMesh.triangles;
            trianglesBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
            trianglesBuffer.SetData(triangles);

            // Set up compute shader parameters.
            computeShader.SetBuffer(kernelIndex, VerticesId, vertexBuffer);
            computeShader.SetBuffer(kernelIndex, TrianglesId, trianglesBuffer);
            computeShader.SetBuffer(kernelIndex, PositionsId, positionsBuffer);
            computeShader.SetInt(ResolutionId, resolution);
            computeShader.SetInt(TrianglesCountId, triangles.Length);

            // Set scale factor based on the size of the mesh.
            var size = currentMesh.bounds.size;
            var sizeMagnitude = size.magnitude;
            var magnitude = Mathf.Clamp(sizeMagnitude, 4, 5);
            computeShader.SetFloat(ScaleId, magnitude / sizeMagnitude);

            // Calculate the number of points per triangle and dispatch the compute shader.
            var numPerTri = (int)(resolution * resolution / (triangles.Length / 3f));
            groupX = Mathf.CeilToInt(triangles.Length / 3f / 10f);
            groupY = Mathf.CeilToInt(numPerTri / 10f);
            totalPointsCount = numPerTri * (triangles.Length / 3) - 1;

            computeShader.SetInt(TotalCountId, totalPointsCount);
            computeShader.SetInt(PointsPerTriangleId, numPerTri);
            computeShader.Dispatch(kernelIndex, groupX, groupY, 1);

            // Calculate step size and set material properties.
            step = 2f / Mathf.Lerp(950, 250, resolution / vertexCount);
            material.SetBuffer(PositionsId, positionsBuffer);
            material.SetFloat(StepId, step);
        }

        /// <summary>
        /// Initializes the target buffers for vertex transformation.
        /// </summary>
        private void InitTargetBuffersIndex()
        {
            if (isTransitioning)
            {
                // Complete transition, update current mesh index.
                isTransitioning = false;
                currentMeshIndex = targetMeshIndex;
            }
            else
            {
                // Select a random target mesh different from the current one.
                do
                {
                    targetMeshIndex = Random.Range(0, meshes.Count);
                } while (currentMeshIndex == targetMeshIndex);

                // Release existing buffers, reset transition parameters.
                ReleaseBuffers();
                transitionDuration = 0;

                // Set up buffers for the new target mesh.
                var targetMesh = meshes[targetMeshIndex];
                vertexBuffer = new ComputeBuffer(targetMesh.vertexCount, 3 * sizeof(float));
                vertexBuffer.SetData(targetMesh.vertices);

                var triangles = targetMesh.triangles;
                trianglesBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
                trianglesBuffer.SetData(triangles);

                // Set up compute shader parameters for transformation.
                computeShader.SetBuffer(TransformKernelIndex, VerticesId, vertexBuffer);
                computeShader.SetBuffer(TransformKernelIndex, TrianglesId, trianglesBuffer);
                computeShader.SetInt(TrianglesCountId, trianglesBuffer.count);
                computeShader.SetBuffer(TransformKernelIndex, PositionsId, positionsBuffer);

                // Set scale factor based on the size of the mesh.
                var size = targetMesh.bounds.size;
                var sizeMagnitude = size.magnitude;
                var magnitude = Mathf.Clamp(sizeMagnitude, 4f, 5f);
                computeShader.SetFloat(ScaleId, magnitude / sizeMagnitude);

                isTransitioning = true;

                // Apply easing to the transition and set up animation sequence.
                var random = Random.value;
                var ease =
                    random < 0.25f ? Ease.InCubic :
                    random < 0.50f ? Ease.InExpo :
                    random < 0.75f ? Ease.InBack :
                    Ease.InSine;

                var targetCore = DOTween.To(() => transitionDuration, duration => transitionDuration = duration, 1f, 2f)
                    .SetEase(ease)
                    .OnUpdate(CalculateTransitioning);

                DOTween.Sequence()
                    .Append(targetCore)
                    .AppendInterval(1.5f)
                    .AppendCallback(InitTargetBuffersIndex)
                    .AppendInterval(0.5f)
                    .OnComplete(InitTargetBuffersIndex);

                // Adjust overshoot for the InBack easing.
                if (ease == Ease.InBack)
                {
                    targetCore.easeOvershootOrAmplitude = 0.6f;
                }
            }
        }

        /// <summary>
        /// Called when the script instance is being destroyed.
        /// </summary>
        private void OnDisable()
        {
            ReleaseBuffers();
            if (positionsBuffer != null)
            {
                positionsBuffer.Release();
                positionsBuffer = null;
            }
        }

        /// <summary>
        /// Releases compute buffers used for GPU calculations.
        /// </summary>
        private void ReleaseBuffers()
        {
            if (vertexBuffer != null)
            {
                vertexBuffer.Release();
                vertexBuffer = null;
            }

            if (trianglesBuffer != null)
            {
                trianglesBuffer.Release();
                trianglesBuffer = null;
            }

            if (totalArea != null)
            {
                totalArea.Release();
                totalArea = null;
            }

            if (triAngle != null)
            {
                triAngle.Release();
                triAngle = null;
            }
        }

        /// <summary>
        /// Calculates the transitioning between two models over time.
        /// </summary>
        private void CalculateTransitioning()
        {
            var numPerTri = (int)(resolution * resolution / (trianglesBuffer.count * 0.33333f));
            groupX = Mathf.CeilToInt(trianglesBuffer.count * 0.033333f);
            groupY = Mathf.CeilToInt(numPerTri * 0.1f);
            computeShader.SetFloat(TransitionProgressId, transitionDuration);
            computeShader.SetInt(PointsPerTriangleId, numPerTri);
            computeShader.Dispatch(TransformKernelIndex, groupX, groupY, 1);

            var totalCount = numPerTri * (trianglesBuffer.count * 0.33333f) - 1;
            var progress = (transitionDuration - 0.5f) * 4f;

            // Adjust total points count based on the transition progress.
            if (totalCount < totalPointsCount)
            {
                if (transitionDuration * 4f <= 1)
                {
                    totalPointsCount = (int)Mathf.Lerp(totalPointsCount, (int)totalCount, transitionDuration * 4f);
                }
            }
            else
            {
                if (transitionDuration >= 0.5f && progress <= 1)
                {
                    totalPointsCount = (int)Mathf.Lerp(totalPointsCount, (int)totalCount, progress);
                }
            }

            // Interpolate vertex count, step size, and update material properties.
            vertexCount = Mathf.Lerp(vertexCount, vertexBuffer.count, transitionDuration);
            var lerp = Mathf.Lerp(300, 250, resolution / vertexCount);
            step = 2f / lerp;
            material.SetBuffer(PositionsId, positionsBuffer);
            material.SetFloat(StepId, step);
        }

        /// <summary>
        /// Called every frame, updates the rendered instanced procedural geometry.
        /// </summary>
        private void Update()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one * 0);
            Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, totalPointsCount);
        }
    }
}
