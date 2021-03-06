﻿using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ParticleBillboard : MonoBehaviour
{
    public float startDelay         = 0.0f;
    public float startLifetime      = 5.0f;
    public float startSpeed         = 5.0f;
    public float startSize          = 1.0f;
    public int   maxParticles       = 1000;

    public Color startColor         = Color.white;
    public Texture2D texture;
    public float gravityModifier    = 0.0f;    

    [System.Serializable]
    public struct Emission{
        public float rateOverTime;
    }
    public Emission emission;
    
    public enum ShapeType{ Cone, Sphere }
    public ShapeType shape;

    [System.Serializable]
    public struct Cone{
        public float angle;
    }
    public Cone cone;
    
    [System.Serializable]
    public struct Sphere{}
    public Sphere sphere;
    
    [System.Serializable]
    public struct Collision{
        public MeshFilter[] planes; // Unity cannot serialize "Plane"
    }
    public Collision collision;

    private Mesh mesh;
    private static int MAX_COLLISION_PLANES = 4;
    
    void Awake()
    {
        GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/ParticleBillboard"));
        
        EditorApplication.update += () => EditorUtility.SetDirty(this);

        setMesh();
        updateUniforms();
    }
    
    private void setMesh()
    {
        if (mesh == null) {
#if UNITY_EDITOR
            MeshFilter mf = GetComponent<MeshFilter>();
            Mesh meshCopy = Mesh.Instantiate(mf.sharedMesh) as Mesh;
            mesh = mf.mesh = meshCopy;
            mesh.name = "Particle Quad (Editor)";
#else
            mesh = GetComponent<MeshFilter>().mesh;
#endif
        }
        var pool = ParticleMeshPool.GetPool();

        mesh.Clear();
        mesh.vertices  = pool.pos;
        mesh.uv        = pool.uv ;
        mesh.triangles = pool.tri;
        mesh.uv2       = pool.id ;
    }

    void updateUniforms()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer.sharedMaterial == null){
            return;
        }
        Material tempMat = new Material(renderer.sharedMaterial);

        tempMat.SetFloat("_StartSize", startSize);
        tempMat.SetFloat("_RateOverTime", emission.rateOverTime);
        tempMat.SetFloat("_StartSpeed", startSpeed);
        tempMat.SetFloat("_StartLifeTime", startLifetime);
        tempMat.SetFloat("_StartDelay", startDelay);        
        tempMat.SetInt("_MaxParticles", maxParticles);
        tempMat.SetFloat("_ConeAngle", cone.angle);
        tempMat.SetVector("_StartColor", startColor);
        tempMat.SetFloat("_GravityModifier", gravityModifier);
        tempMat.SetTexture("_Texture", texture);

        updateCollisionPlanes(tempMat);
        updateKeywords(tempMat);
        
        renderer.material = tempMat;
        
        if (mesh != null){
            float bound_len = startLifetime * startSpeed;
            mesh.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(bound_len, bound_len, bound_len));
        }
    }

    void updateCollisionPlanes(Material mat)
    {
        for (int i = 0; i < MAX_COLLISION_PLANES; i++){

            if (collision.planes.Length > i && collision.planes[i] != null) {
                Vector3 plane_center = collision.planes[i].transform.position;
                Vector3 plane_up = collision.planes[i].transform.up;
                
                Vector4 plane_center4 = new Vector4(plane_center.x, plane_center.y, plane_center.z, 1.0f);
                Vector4 plane_normal4 = new Vector4(plane_up.x, plane_up.y, plane_up.z, 1.0f);

                Vector3 normal = Vector3.Normalize(plane_normal4);
                float plane_d = -1 * Vector3.Dot(normal, plane_center4);
                Vector4 plane_eq = new Vector4(normal.x, normal.y, normal.z, plane_d);

                mat.SetVector("_CollisionPlaneEquation" + i, plane_eq);
                mat.EnableKeyword("COL_PLANE_" + i);
            } else {
                mat.DisableKeyword("COL_PLANE_" + i);
            }
        }   
    }

    void updateKeywords(Material mat)
    {
        if (mat.GetTexture("_Texture") != null){
            mat.EnableKeyword("FRAG_TEXTURE");
        } else {
            mat.DisableKeyword("FRAG_TEXTURE");
        }

        if (shape == ShapeType.Sphere) {
            mat.EnableKeyword("SHAPE_SPHERE");
        }else {
            mat.DisableKeyword("SHAPE_SPHERE");
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Call updateUniforms() if any of collision planes was changed.
        for (int i = 0; i < MAX_COLLISION_PLANES; i++){
            if (collision.planes.Length > i && collision.planes[i] != null){
                if (collision.planes[i].transform.hasChanged){
                    collision.planes[i].transform.hasChanged = false;
                    updateUniforms();
                    break;
                }
            }
        }
    }

    void OnValidate()
    {
        if (collision.planes != null && collision.planes.Length > MAX_COLLISION_PLANES) {
            Debug.LogWarning("Up to " + MAX_COLLISION_PLANES + " collision planes!");
            System.Array.Resize(ref collision.planes, MAX_COLLISION_PLANES);
        }

        updateUniforms();
    }
#endif
}