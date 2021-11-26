using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Optimization;
using MathNet.Numerics;
using Mediapipe;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
public class Fitskinmesh : MonoBehaviour
{
  public Camera target_camera;
  public List<NormalizedLandmarkList> landmarks;
  private readonly int[] _indexOnface = new int[] { 4385, 5266, 4466, 5210, 880, 4251, 217, 1892, 389, 3899, 225, 523,
        1648, 3628, 12, 1621, 3493, 3427, 3471, 2299 };
  private readonly int[] _indexOnlandmarks = new int[] { 10, 152, 234, 454, 1, 168, 61, 291, 0, 13, 14, 17,
        33, 133, 159, 145, 362, 263, 386, 374 };
  /// <summary>
  /// 边缘 10 152 234 454 21 172 397 1 168
  /// </summary>

  //眼
  //private readonly int[] _indexOnface = new int[] { 1648,3628,1360,1621,3493,3427,3471,3402 };
  //private readonly int[] _indexOnlandmarks = new int[] { 33, 133, 159, 145, 362, 263, 386, 374 };
  //嘴
  private readonly int[] _indexOnfacemouth = new int[] { 217, 1892, 389, 3899, 225, 523 };
  private readonly int[] _indexOnlandmarksmouth = new int[] { 61, 291, 0, 13, 14, 17 };
  //
  //private readonly int[] _indexOnlandmarks = new int[] { 61, 391, 0, 13, 14, 17 };


  SkinnedMeshRenderer skinned;
  Mesh m;

  Vector3[] delv;
  Vector3[] deln;
  Vector3[] delt;
  // Start is called before the first frame update
  void Start()

  {
   
    
    t = transform;
    skinned = GetComponent<SkinnedMeshRenderer>();
    m = new Mesh();
    //target_camera = Camera.main;
    Guess = new DenseVector(new[] { transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z,
            transform.position.x, transform.position.y, (double)transform.position.z, (double)skinned.GetBlendShapeWeight((int)BlendShpeKey.jawOpen) });
    
    delv = new Vector3[skinned.sharedMesh.vertexCount];
    deln = new Vector3[skinned.sharedMesh.normals.Length];
    delt = new Vector3[skinned.sharedMesh.tangents.Length];
    skinned.sharedMesh.GetBlendShapeFrameVertices((int)BlendShpeKey.jawOpen, 0, delv, deln, delt);
    if (landmarks != null)
    {
      InstXY();
      Process();
    }
    skinned.BakeMesh(m);
    faceVerts = m.vertices;
    _lastInputY = new Vector2[_indexOnface.Length];
  }


  // Update is called once per frame
  void Update()
  {
    UpdateMesh();
  }



  void UpdateMesh()
  {
    if (landmarks != null)
    {
      InstXY();
      Process();
    }

  }


  DenseVector Guess;
  Vector4[] inputX;
  Vector2[] inputY;
  Vector2[] _lastInputY;
  Vector3[] faceVerts;
  [Range(0,1)]
  public float rate =0.5f;
  private void InstXY()
  {
    skinned.BakeMesh(m);
    faceVerts = m.vertices;
    inputX = new Vector4[_indexOnface.Length];
    inputY = new Vector2[_indexOnface.Length];
    for (var i = 0; i < _indexOnface.Length; i++)
    {
      var selectX = faceVerts[_indexOnface[i]];
      inputX[i] = new Vector4(selectX.x, selectX.y, selectX.z, 1);
      if (landmarks!=null)
      {
        var selectY = landmarks[0].Landmark[_indexOnlandmarks[i]];
        inputY[i] = _lastInputY[i]*rate+  new Vector2(selectY.X * target_camera.pixelWidth, selectY.Y * target_camera.pixelHeight)*(1-rate);
      }

    }
    _lastInputY = inputY;
  }
  DenseVector downGuess = new DenseVector(new double[] {0,0,0,-100,-100,-100,0 });
  DenseVector upGuess = new DenseVector(new double[] {360,360,360,100,100,100,100 });

  private void Process()
  {

    double Value(Vector<double> input)
    {
      Vector3 rotate = new Vector3(((float)input[0]), ((float)input[1]), ((float)input[2]));
      Vector3 translate = new Vector3(((float)input[3]), ((float)input[4]), ((float)input[5]));
      //input[6] = Mathf.Clamp((float)input[6], 0f, 100);
      return GetValue(translate, rotate, inputX, inputY, input[6]);
    }
    var obj = ObjectiveFunction.Value(Value);
    var solver = new NelderMeadSimplex(convergenceTolerance: 1E-08, maximumIterations: 10000000);
    var initialGuess = Guess;
    var result = solver.FindMinimum(obj, initialGuess);
    Guess = new DenseVector(new[] { result.MinimizingPoint[0], result.MinimizingPoint[1], result.MinimizingPoint[2],
            result.MinimizingPoint[3], result.MinimizingPoint[4], result.MinimizingPoint[5],Mathf.Clamp((float)result.MinimizingPoint[6],0,100) });
    t.eulerAngles = new Vector3((float)result.MinimizingPoint[0], (float)result.MinimizingPoint[1], (float)result.MinimizingPoint[2]);
    t.position = new Vector3((float)result.MinimizingPoint[3], (float)result.MinimizingPoint[4], (float)result.MinimizingPoint[5]);
    skinned.SetBlendShapeWeight((int)BlendShpeKey.jawOpen, (float)result.MinimizingPoint[6]);
  }
  float GetValue(Vector3 translate, Vector3 rotate, Vector4[] x, Vector2[] y,double blendshapevalue)
  {
    float loss = 0;
    for (int i = 0; i < x.Length; i++)
    {
      Vector3 v = faceVerts[_indexOnface[i]] + delv[_indexOnface[i]] * (float)blendshapevalue / 100;
      x[i] = new Vector4(v.x, v.y, v.z, 1);
    }

    for (int i = 0; i < y.Length; i++)
    {
      loss += Mathf.Sqrt(Residuals(rotate, translate, x[i], y[i]));
    }
    loss /= y.Length;
    return loss;
  }

  Transform t;
  float Residuals(Vector3 rotation, Vector3 translate, Vector4 x, Vector2 y)
  {
    float loss = 0;
    t.eulerAngles = rotation;
    t.position = translate;
    Vector2 screenValue = GetPoint(x, t);
    loss = (y - screenValue).sqrMagnitude;
    //Debug.Log("y" + y + " " + "getpoint" + screenValue+"loss"+ loss);
    return loss;
  }
  Vector2 GetPoint(Vector4 point, Transform t)
  {
    Vector2 endvalue;
    Vector4 v = target_camera.projectionMatrix * target_camera.worldToCameraMatrix * t.localToWorldMatrix * point;
    endvalue = new Vector4(v.x * target_camera.pixelWidth / (2 * v.w) + target_camera.pixelWidth / 2, v.y * target_camera.pixelHeight
        / (2 * v.w) + target_camera.pixelHeight / 2);
    endvalue = new Vector2(endvalue.x, target_camera.pixelHeight - endvalue.y);
    return endvalue;
  }
}
