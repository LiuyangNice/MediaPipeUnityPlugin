using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Optimization;
using Mediapipe;

public class Test : MonoBehaviour
{
  public Camera camera;
  private Mesh mesh;
  //边缘
  private readonly int[] _indexOnface = new int[] { 4385, 5270, 4466, 5212, 3216, 3268, 1510, 3170, 4928, 4251, 1648, 3628, 1360, 1621, 3493, 3427, 3471, 3402, 452, 2618, 382, 3897, 225, 416 };
  private readonly int[] _indexOnlandmarks = new int[] { 10, 152, 234, 454, 21, 251, 172, 397, 1, 168, 33, 133, 159, 145, 362, 263, 386, 374, 61, 391, 0, 13, 14, 17 };
  //眼
  //private readonly int[] _indexOnface = new int[] { 1648,3628,1360,1621,3493,3427,3471,3402 };
  //private readonly int[] _indexOnlandmarks = new int[] { 33, 133, 159, 145, 362, 263, 386, 374 };
  //嘴
  //private readonly int[] _indexOnface = new int[] { 452, 2618, 382, 3897, 225, 416 };
  //private readonly int[] _indexOnlandmarks = new int[] { 61, 391, 0, 13, 14, 17 };
  //
  //private readonly int[] _indexOnlandmarks = new int[] { 61, 391, 0, 13, 14, 17 };
  Vector4[] v4 = new Vector4[6];
  Vector2[] v2 = new Vector2[6];
  public List<NormalizedLandmarkList> multiFaceLandmarks;

  // Start is called before the first frame update
  void Start()
  {
    t = transform;
    mesh = GetComponent<MeshFilter>().mesh;
    for (int i = 0; i < 6; i++)
    {
      v4[i] = new Vector4(mesh.vertices[_indexOnface[i]].x, mesh.vertices[_indexOnface[i]].y, mesh.vertices[_indexOnface[i]].z, 1);
    }
    last = new DenseVector(new[] { (double)transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z,
      transform.position.x, transform.position.y, transform.position.z });
  }
  private void Update()
  {
    if (multiFaceLandmarks != null)
    {
      GetRt(multiFaceLandmarks);
    }
  }
  public void GetRt(List<NormalizedLandmarkList> multiFaceLandmarks)
  {
    for (int i = 0; i < 6; i++)
    {
      v2[i] = new Vector2(multiFaceLandmarks[0].Landmark[_indexOnlandmarks[i]].X * camera.pixelWidth,
        multiFaceLandmarks[0].Landmark[_indexOnlandmarks[i]].Y * camera.pixelHeight);
    }
    Process();
  }

  public Vector2 pos;
  Transform t;
  DenseVector last;
  void Process()
  {
    double Value(Vector<double> input)
    {
      var rotate = new Vector3((float)input[0], (float)input[1], (float)input[2]);
      var translate = new Vector3((float)input[3], (float)input[4], (float)input[5]);
      return GetValue(translate, rotate, v4, v2);
    }
    var obj = ObjectiveFunction.Value(Value);
    var solver = new NelderMeadSimplex(convergenceTolerance: 0.0000000001, maximumIterations: 1000000000);
   
    var initialGuess = last;

    var result = solver.FindMinimum(obj, initialGuess);
    last = new DenseVector(result.MinimizingPoint.ToArray());

    t.eulerAngles = new Vector3((float)result.MinimizingPoint[0], (float)result.MinimizingPoint[1], (float)result.MinimizingPoint[2]);
    t.position = new Vector3((float)result.MinimizingPoint[3], (float)result.MinimizingPoint[4], (float)result.MinimizingPoint[5]);
  }

  private float GetValue(Vector3 translate, Vector3 rotate, Vector4[] x, Vector2[] y)
  {
    float loss = 0;
    for (int i = 0; i < y.Length; i++)
    {
      loss += Mathf.Sqrt(Residuals(rotate, translate, x[i], y[i]));
    }
    loss /= y.Length;
    return loss;
  }


  float Residuals(Vector3 rotation, Vector3 translate, Vector4 x, Vector2 y)
  {
    float loss = 0;
    t.eulerAngles = rotation;
    t.position = translate;
    Vector2 screenValue = GetPoint(x, t);
    loss = (y - screenValue).sqrMagnitude;
    return loss;
  }
  Vector2 GetPoint(Vector4 point, Transform t)
  {
    Vector2 endvalue;
    Vector4 v = camera.projectionMatrix * camera.worldToCameraMatrix * t.localToWorldMatrix * point;
    endvalue = new Vector4(v.x * camera.pixelWidth / (2 * v.w) + camera.pixelWidth / 2, v.y * camera.pixelHeight
        / (2 * v.w) + camera.pixelHeight / 2);
    endvalue = new Vector2(endvalue.x, camera.pixelHeight - endvalue.y);
    return endvalue;
  }
}
