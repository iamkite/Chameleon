using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//shapeof 만 바꿔주면 됩니당

//1. 왼쪽으로 갈때 triangle 구조를 바꿔줄건데 애니메이션은 어떻게 할것인가?????????????????

public class NY_StrokeL : MonoBehaviour
{


    //밖에서 넣어줄것들
    public GameObject strokeMaker;
    public float defaultsize = 0.03f;
    private Transform hand;
    public Transform output;
    public Material[] material;
    public GameObject emptyObject;

    //소리
    AudioSource[] audios;
    OVRHapticsClip clip;

    //그리기 편집
    public float size;
    public Color color = new Color(1, 1, 1);
    //public GameObject shapes;

    //애니메이션
    public AnimationCurve ac;
    public float changeStartSize = 1 / 3f;
    public float changeStartSizeSpeed = 0.03f;

    //현재strokeMaker 관리
    private GameObject curCube;
    private int curShapeof;
    private Vector3 curHandPos;
    private Vector3[] curVertexOfShape;

    //현재strokeMaker 속성
    private Mesh mesh;
    private List<Vector3> vertices;
    private List<int> triangles;
    private List<Color> colors;
    private Color thisColor;
    private int verticesNum = 0;
    private MeshCollider mc;
    private List<FollowingVertex> follow;
    //private Vector3 curMakerPos;
    Vector3 dif;
    Vector3 center;

    //ChangeSize
    public float sizelimitup = 0.03f * 3f;
    public float sizelimitdown = 0.03f / 2f;
    float tempSize;
    bool sizeWaiting;

    //만들어지는 모양
    public Vector3[][] locationsOfVertices = new Vector3[6][];
    public float createDistance = 0.03f;
    // 1 : right -1 : left
    private int whichWay = 0;
    private bool waychanged;

    //using shader
    Color curcolor;
    int tt = 0;

    //added
    Texture2D curTexture;
    Color[] textureColor;

    public enum Shapes
    {
        Triangle,
        Square,
        Pentagon,
        Hexagon,
        Octagon,
        Hexagonal,
    }
    public Shapes shapeof;

    public static NY_StrokeL Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    // Use this for initialization
    void Start()
    {
        hand = GameManager.Instance.LPoint;
        audios = GetComponents<AudioSource>();
        clip = new OVRHapticsClip(audios[1].clip);
        shapeof = Shapes.Triangle;
        curShapeof = (int)shapeof;
        size = defaultsize;
        tempSize = defaultsize;
        vertices = new List<Vector3>();
        triangles = new List<int>();
        colors = new List<Color>();
        follow = new List<FollowingVertex>();
        thisColor = color;
        //정점 박아두기 : x는 무조건 0, y는 y, z는 x
        for (int i = 3; i < 9; i++)
        {
            if (i < 7)
            {
                List<Vector3> temp = new List<Vector3>();
                for (int j = 0; j < i; j++)
                {
                    float ratio = (float)j / (i);
                    float rad = ratio * Mathf.PI * 2;

                    float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
                    float x = cos, y = sin;
                    temp.Add(new Vector3(0, y, x));
                }
                locationsOfVertices[i - 3] = temp.ToArray();
            }
            else if (i == 7)
            {
                List<Vector3> temp = new List<Vector3>();
                for (int j = 0; j < 8; j++)
                {
                    float ratio = (float)j / (8);
                    float rad = ratio * Mathf.PI * 2;

                    float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
                    float x = cos, y = sin;
                    temp.Add(new Vector3(0, y, x));
                }
                locationsOfVertices[i - 3] = temp.ToArray();
            }
            else
            {
                List<Vector3> temp = new List<Vector3>();
                for (int j = 0; j < 20; j++)
                {
                    float ratio = (float)j / (20);
                    float rad = ratio * Mathf.PI * 2;

                    float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
                    float x = cos, y = sin;
                    temp.Add(new Vector3(0, y, x));
                }
                locationsOfVertices[i - 3] = temp.ToArray();
            }
        }
        //정점 초기화
        curVertexOfShape = new Vector3[locationsOfVertices[curShapeof].Length];
        for (int i = 0; i < locationsOfVertices[curShapeof].Length; i++)
        {
            //정점 사이즈 바꿔주기
            curVertexOfShape[i] = locationsOfVertices[curShapeof][i] * size;
        }

    }

    // Update is called once per frame
    void Update()
    {
        //굳이 계속 색깔 받아올 필요 없을것 같아서 옮겼다!
        //color = GameManager.Instance.color;
        if (GameManager.Instance.view == GameManager.View.Wireframe)
        {
            color = GameManager.Instance.color;

            //stroke
            //getdown시 처음 도형 만들기
            if (curCube == null && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            {
                //audios[0].Play();
                GameObject s = Instantiate(strokeMaker);
                curCube = s;
                s.transform.position = hand.transform.position;
                s.transform.rotation = hand.transform.rotation;
                s.transform.parent = output;
                emptyObject.transform.position = curCube.transform.position;
                emptyObject.transform.rotation = curCube.transform.rotation;

                Material[] mts = new Material[2];
                mts[0] = material[1];
                mts[1] = GameManager.Instance.mtWireFrame;
                s.GetComponent<MeshRenderer>().materials = mts;

                mesh = s.GetComponent<MeshFilter>().mesh;
                mc = s.GetComponent<MeshCollider>();

                thisColor = color;

                vertices.Clear();
                triangles.Clear();
                colors.Clear();
                follow.Clear();
                verticesNum = 0;

                //make new list of vertices if number of shapeof is changed
                if ((int)shapeof != curShapeof)
                {
                    curShapeof = (int)shapeof;
                    //makelist
                    curVertexOfShape = new Vector3[locationsOfVertices[curShapeof].Length];
                    for (int i = 0; i < locationsOfVertices[curShapeof].Length; i++)
                    {
                        //정점 사이즈 바꿔주기
                        curVertexOfShape[i] = locationsOfVertices[curShapeof][i] * size;
                    }
                }


                CreateAnim(curVertexOfShape.Length);


            }
            //도형 잇기
            else if (curCube != null && OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch) && Vector3.Distance(emptyObject.transform.position, hand.transform.position) > createDistance)
            {
                audios[1].Play();
                OVRHaptics.LeftChannel.Preempt(clip);
                //방향 인식하기

                //오른쪽으로 갔다면
                if (whichWay != 1 && emptyObject.transform.InverseTransformPoint(hand.transform.position).x >= 0)
                {
                    if (whichWay == -1)
                    {
                        waychanged = true;
                    }
                    whichWay = 1;
                }
                else if (whichWay != -1 && emptyObject.transform.InverseTransformPoint(hand.transform.position).x < 0)
                {
                    if (whichWay == 1)
                    {
                        waychanged = true;
                    }
                    whichWay = -1;
                }

                emptyObject.transform.position = hand.transform.position;
                emptyObject.transform.rotation = hand.transform.rotation;
                CreateAnim(curVertexOfShape.Length);
            }
            //끝내기 및 초기화
            else if (curCube != null && OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            {
                if (output.transform.childCount > 1)
                {
                    NY_StrokeR.Instance.set();
                }
                audios[1].Stop();
                audios[2].Play();
                OVRHaptics.LeftChannel.Preempt(clip);
                //중심 바꿔주기
                dif = curCube.transform.position - curCube.transform.TransformPoint(mesh.bounds.center);
                for (int i = 0; i < vertices.Count; i++)
                {
                    vertices[i] += curCube.transform.InverseTransformVector(dif);
                }
                curCube.transform.position = curCube.transform.TransformPoint(mesh.bounds.center);

                mesh.Clear();
                mesh.vertices = vertices.ToArray();
                mesh.triangles = triangles.ToArray();
                mesh.colors = colors.ToArray();
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mc.sharedMesh = mesh;

                whichWay = 0;
                NY_Undo.work.Push(0);
                NY_Undo.createdobj.Push(curCube);
                NY_Undo.redo = false;
                curCube = null;
            }
        }
        else
        {
            //stroke
            //getdown시 처음 도형 만들기
            if (curCube == null && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            {
                color = GameManager.Instance.color;
                GameObject s = Instantiate(strokeMaker);
                curCube = s;
                s.transform.position = hand.transform.position;
                s.transform.rotation = hand.transform.rotation;
                s.transform.parent = output;
                emptyObject.transform.position = curCube.transform.position;
                emptyObject.transform.rotation = curCube.transform.rotation;

                s.GetComponent<MeshRenderer>().material = material[0];

                mesh = s.GetComponent<MeshFilter>().mesh;
                mc = s.GetComponent<MeshCollider>();

                thisColor = color;

                curTexture = new Texture2D(256, 256);
                textureColor = new Color[256 * 256];

                curCube.GetComponent<MeshRenderer>().material.mainTexture = curTexture;
                for (int i = 0; i < 256 * 256; i++)
                {
                    textureColor[i] = color;
                }

                curTexture.SetPixels(textureColor);
                curTexture.Apply();


                vertices.Clear();
                triangles.Clear();
                colors.Clear();
                follow.Clear();
                verticesNum = 0;

                //make new list of vertices if number of shapeof is changed
                if ((int)shapeof != curShapeof)
                {
                    curShapeof = (int)shapeof;
                    //makelist
                    //curVertexOfShape = locationsOfVertices[curShapeof];
                    curVertexOfShape = new Vector3[locationsOfVertices[curShapeof].Length];
                    for (int i = 0; i < locationsOfVertices[curShapeof].Length; i++)
                    {
                        //정점 사이즈 바꿔주기
                        curVertexOfShape[i] = locationsOfVertices[curShapeof][i] * size;
                    }
                }


                Create(curVertexOfShape.Length);


            }
            //도형 잇기
            else if (curCube != null && OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch) && Vector3.Distance(emptyObject.transform.position, hand.transform.position) > createDistance)
            {
                OVRHaptics.LeftChannel.Preempt(clip);
                audios[1].Play();
                //방향 인식하기

                //오른쪽으로 갔다면
                if (whichWay != 1 && emptyObject.transform.InverseTransformPoint(hand.transform.position).x >= 0)
                {
                    if (whichWay == -1)
                    {
                        waychanged = true;
                    }
                    whichWay = 1;
                }
                else if (whichWay != -1 && emptyObject.transform.InverseTransformPoint(hand.transform.position).x < 0)
                {
                    if (whichWay == 1)
                    {
                        waychanged = true;
                    }
                    whichWay = -1;
                }

                emptyObject.transform.position = hand.transform.position;
                emptyObject.transform.rotation = hand.transform.rotation;
                Create(curVertexOfShape.Length);
                //audio[1].Stop();
            }
            //끝내기 및 초기화
            else if (curCube != null && OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            {
                if (output.transform.childCount > 1)
                {
                    NY_StrokeR.Instance.set();
                }
                audios[1].Stop();
                audios[2].Play();
                OVRHaptics.LeftChannel.Preempt(clip);
                //중심 바꿔주기
                dif = curCube.transform.position - curCube.transform.TransformPoint(mesh.bounds.center);
                for (int i = 0; i < vertices.Count; i++)
                {
                    vertices[i] += curCube.transform.InverseTransformVector(dif);
                    colors[i] += new Color(curCube.transform.InverseTransformVector(dif).x, curCube.transform.InverseTransformVector(dif).y, curCube.transform.InverseTransformVector(dif).z, 0);
                }
                curCube.transform.position = curCube.transform.TransformPoint(mesh.bounds.center);

                mesh.Clear();
                mesh.vertices = vertices.ToArray();
                mesh.triangles = triangles.ToArray();
                mesh.colors = colors.ToArray();
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mc.sharedMesh = mesh;



                if (mesh.colors[mesh.colors.Length - 1].a < 1)
                {
                    StartCoroutine(moving(curCube, curVertexOfShape.Length, verticesNum, thisColor));
                }
                else
                {
                    colors.Clear();
                    for (int i = 0; i < mesh.colors.Length; i++)
                    {
                        colors.Add(thisColor);
                    }

                    curCube.GetComponent<MeshRenderer>().material = material[1];
                    mesh.Clear();
                    mesh.vertices = vertices.ToArray();
                    mesh.triangles = triangles.ToArray();
                    mesh.colors = colors.ToArray();
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();
                    mc.sharedMesh = mesh;
                }

                whichWay = 0;
                NY_Undo.work.Push(0);
                NY_Undo.createdobj.Push(curCube);
                NY_Undo.redo = false;
                curCube = null;
            }

            if (curCube != null)
            {
                AddAlpha(curVertexOfShape.Length);
            }
        }



        //크기조절
        if (curCube != null && !sizeWaiting)
        {
            if (OVRInput.Get(OVRInput.Button.PrimaryThumbstickUp, OVRInput.Controller.RTouch))
            {
                StartCoroutine(ThumbUp());
            }
            else if (OVRInput.Get(OVRInput.Button.PrimaryThumbstickDown, OVRInput.Controller.RTouch))
            {
                StartCoroutine(Thumbdown());
            }
        }
    }

    void AddAlpha(int num)
    {
        Color[] tempcolor = mesh.colors;

        if (tempcolor[0].a < 1)
        {
            for (int i = 0; i < num; i++)
            {
                tempcolor[i].a += 0.03f;
            }
        }
        // 그 다음부터는
        //else
        {
            if (verticesNum > num)
            {
                for (int i = num; i < verticesNum - 6 * num; i++)
                {
                    //print("first" + i);
                    tt++;
                    if (tempcolor[i].a < 1)
                    {
                        tempcolor[i].a += 0.03f;
                        //옆면아이들 따라오게하기
                        for (int j = 0; j < 3; j++)
                        {
                            tempcolor[follow[(i / (num * 7) + 1) * num + (i % (num * 7) - num)].frontVertex[j]].a = tempcolor[i].a;
                        }
                    }
                    if (tt == num) { i += 6 * num; tt = 0; }
                }
            }
            tt = 0;

            //전전 면의 backvertex들을 전전면의 vertex가 움직이는대로 움직이기
            if (verticesNum >= num * 8)
            {
                for (int i = 0; i < num; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        tempcolor[follow[(i / (num * 7) + 1) * num + (i % (num * 7) - num)].backVertex[j]].a = tempcolor[i].a;
                    }
                }
            }
            if (verticesNum >= num * 14)
            {
                for (int i = num; i < verticesNum - 13 * num; i++)
                {
                    tt++;
                    for (int j = 0; j < 3; j++)
                    {
                        tempcolor[follow[(i / (num * 7) + 1) * num + (i % (num * 7) - num)].backVertex[j]].a = tempcolor[i].a;
                    }
                    //print("second" + i + " " + tt);
                    if (tt == num) { i += 6 * num; tt = 0; }
                }
            }

            tt = 0;
        }

        colors.Clear();
        colors.AddRange(tempcolor);
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = tempcolor;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    IEnumerator moving(GameObject tempCube, int num, int tempverticesNum, Color tempColor)
    {
        Mesh tempmesh = tempCube.GetComponent<MeshFilter>().mesh;
        List<FollowingVertex> f = new List<FollowingVertex>(follow);
        Color[] tempcolor = tempmesh.colors;
        Vector3[] tempvertices = tempmesh.vertices;
        int[] temptriangles = tempmesh.triangles;

        while (tempcolor[tempcolor.Length - 1].a < 1)
        {
            //AddAlpha(tempmesh, num, tempverticesNum);

            if (tempcolor[0].a < 1)
            {
                for (int i = 0; i < num; i++)
                {
                    tempcolor[i].a += 0.03f;
                }
            }
            // 그 다음부터는
            //else
            {
                if (tempverticesNum > num)
                {
                    for (int i = num; i < tempverticesNum - 6 * num; i++)
                    {

                        tt++;

                        if (tempcolor[i].a < 1)
                        {
                            tempcolor[i].a += 0.03f;
                            //옆면아이들 따라오게하기
                            for (int j = 0; j < 3; j++)
                            {
                                //왜 여기서 자꾸 index오류가 뜨지
                                tempcolor[f[(i / (num * 7) + 1) * num + (i % (num * 7) - num)].frontVertex[j]].a = tempcolor[i].a;
                            }
                        }
                        if (tt == num) { i += 6 * num; tt = 0; }
                    }
                }
                tt = 0;

                //전전 면의 backvertex들을 전전면의 vertex가 움직이는대로 움직이기
                if (tempverticesNum >= num * 8)
                {
                    for (int i = 0; i < num; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            tempcolor[f[(i / (num * 7) + 1) * num + (i % (num * 7) - num)].backVertex[j]].a = tempcolor[i].a;
                        }
                    }
                }
                if (tempverticesNum >= num * 14)
                {
                    for (int i = num; i < tempverticesNum - 13 * num; i++)
                    {
                        tt++;
                        for (int j = 0; j < 3; j++)
                        {
                            tempcolor[f[(i / (num * 7) + 1) * num + (i % (num * 7) - num)].backVertex[j]].a = tempcolor[i].a;
                        }
                        //print("second" + i + " " + tt);
                        if (tt == num) { i += 6 * num; tt = 0; }
                    }
                }

                tt = 0;
            }

            tempmesh.Clear();
            tempmesh.vertices = tempvertices;
            tempmesh.triangles = temptriangles;
            tempmesh.colors = tempcolor;
            tempmesh.RecalculateBounds();
            tempmesh.RecalculateNormals();
            yield return new WaitForEndOfFrame();
        }

        for (int i = 0; i < tempcolor.Length; i++)
        {
            tempcolor[i] = tempColor;
        }

        tempCube.GetComponent<MeshRenderer>().material = material[1];
        tempmesh.Clear();
        tempmesh.vertices = tempvertices;
        tempmesh.triangles = temptriangles;
        tempmesh.colors = tempcolor;
        tempmesh.RecalculateBounds();
        tempmesh.RecalculateNormals();
    }


    private void Create(int num)
    {
        //curHandPos = hand.transform.position;
        curcolor.a = 0;
        curcolor.r = curCube.transform.InverseTransformPoint(hand.transform.position).x;
        curcolor.g = curCube.transform.InverseTransformPoint(hand.transform.position).y;
        curcolor.b = curCube.transform.InverseTransformPoint(hand.transform.position).z;


        //면생성
        //add vertex
        for (int i = 0; i < num; i++)
        {
            vertices.Add(curCube.transform.InverseTransformPoint(hand.transform.TransformPoint(curVertexOfShape[i])));
            follow.Add(new FollowingVertex(vertices[vertices.Count - 1]));
            colors.Add(curcolor);
        }

        //방향이 바뀌지 않았을때 그 전면 없애기
        if (!waychanged && verticesNum > num * 10)
        {
            triangles.RemoveRange(triangles.Count - num * 2 * 3 - (num - 2) * 3, (num - 2) * 3);
        }
        else if (waychanged)
        {
            waychanged = false;
        }


        //triangle들을 더해준다
        for (int i = 1; i < num - 1; i++)
        {
            if (whichWay == -1 || whichWay == 0)
            {
                triangles.Add(0 + verticesNum);
                triangles.Add(i + verticesNum);
                triangles.Add(i + 1 + verticesNum);
            }
            else
            {
                triangles.Add(i + 1 + verticesNum);
                triangles.Add(i + verticesNum);
                triangles.Add(0 + verticesNum);
            }
        }

        #region adding vertex
        if (verticesNum == 0)
        {
            verticesNum = num;
        }
        else if (verticesNum == num)
        {
            //왼쪽으로 갈때 triangle 바꿔주기
            if (whichWay == -1)
            {
                triangles.Clear();
                //왼쪽으로 가는것임. --------------> 여기서  첫번째 면의 triangle 순서 바꿔주기
                for (int j = 1; j < num - 1; j++)
                {

                    triangles.Add(j + 1);
                    triangles.Add(j);
                    triangles.Add(0);

                }
            }
            for (int i = 0; i < num; i++)
            {
                int a = i, b = (i + 1) % num, c = i + num, d = (i + 1) % num + num;

                if (whichWay == 1)
                {
                    vertices.Add(vertices[a]);
                    follow[a].AddBack(vertices.Count - 1);
                    colors.Add(mesh.colors[0]);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[c]);
                    follow[c].AddFront(vertices.Count - 1);
                    colors.Add(curcolor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[b]);
                    follow[b].AddBack(vertices.Count - 1);
                    colors.Add(mesh.colors[0]);
                    triangles.Add(vertices.Count - 1);



                    vertices.Add(vertices[d]);
                    follow[d].AddFront(vertices.Count - 1);
                    colors.Add(curcolor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[b]);
                    follow[b].AddBack(vertices.Count - 1);
                    colors.Add(mesh.colors[0]);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[c]);
                    follow[c].AddFront(vertices.Count - 1);
                    colors.Add(curcolor);
                    triangles.Add(vertices.Count - 1);
                }
                else if (whichWay == -1)
                {
                    vertices.Add(vertices[b]);
                    follow[b].AddBack(vertices.Count - 1);
                    colors.Add(mesh.colors[0]);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[c]);
                    follow[c].AddFront(vertices.Count - 1);
                    colors.Add(curcolor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[a]);
                    follow[a].AddBack(vertices.Count - 1);
                    colors.Add(mesh.colors[0]);
                    triangles.Add(vertices.Count - 1);



                    vertices.Add(vertices[c]);
                    follow[c].AddFront(vertices.Count - 1);
                    colors.Add(curcolor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[b]);
                    follow[b].AddBack(vertices.Count - 1);
                    colors.Add(mesh.colors[0]);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[d]);
                    follow[d].AddFront(vertices.Count - 1);
                    colors.Add(curcolor);
                    triangles.Add(vertices.Count - 1);
                }
            }

            verticesNum += num * 7;
        }
        else
        {
            for (int i = verticesNum - num * 7; i < verticesNum - num * 6; i++)
            {
                int a = i, b = (i + 1) % num + verticesNum - num * 7, c = a + num * 7, d = b + num * 7;

                if (whichWay == 1)
                {
                    vertices.Add(vertices[a]);
                    follow[(a / (num * 7) + 1) * num + (a % (num * 7) - num)].AddBack(vertices.Count - 1);
                    colors.Add(mesh.colors[verticesNum - 6 * num - 1]);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[c]);
                    follow[(c / (num * 7) + 1) * num + (c % (num * 7) - num)].AddFront(vertices.Count - 1);
                    colors.Add(curcolor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[b]);
                    follow[(b / (num * 7) + 1) * num + (b % (num * 7) - num)].AddBack(vertices.Count - 1);
                    colors.Add(mesh.colors[verticesNum - 6 * num - 1]);
                    triangles.Add(vertices.Count - 1);



                    vertices.Add(vertices[d]);
                    follow[(d / (num * 7) + 1) * num + (d % (num * 7) - num)].AddFront(vertices.Count - 1);
                    colors.Add(curcolor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[b]);
                    follow[(b / (num * 7) + 1) * num + (b % (num * 7) - num)].AddBack(vertices.Count - 1);
                    colors.Add(mesh.colors[verticesNum - 6 * num - 1]);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[c]);
                    follow[(c / (num * 7) + 1) * num + (c % (num * 7) - num)].AddFront(vertices.Count - 1);
                    colors.Add(curcolor);
                    triangles.Add(vertices.Count - 1);
                }
                else if (whichWay == -1)
                {
                    vertices.Add(vertices[b]);
                    follow[(b / (num * 7) + 1) * num + (b % (num * 7) - num)].AddBack(vertices.Count - 1);
                    colors.Add(mesh.colors[verticesNum - 6 * num - 1]);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[c]);
                    follow[(c / (num * 7) + 1) * num + (c % (num * 7) - num)].AddFront(vertices.Count - 1);
                    colors.Add(curcolor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[a]);
                    follow[(a / (num * 7) + 1) * num + (a % (num * 7) - num)].AddBack(vertices.Count - 1);
                    colors.Add(mesh.colors[verticesNum - 6 * num - 1]);
                    triangles.Add(vertices.Count - 1);


                    vertices.Add(vertices[c]);
                    follow[(c / (num * 7) + 1) * num + (c % (num * 7) - num)].AddFront(vertices.Count - 1);
                    colors.Add(curcolor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[b]);
                    follow[(b / (num * 7) + 1) * num + (b % (num * 7) - num)].AddBack(vertices.Count - 1);
                    colors.Add(mesh.colors[verticesNum - 6 * num - 1]);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[d]);
                    follow[(d / (num * 7) + 1) * num + (d % (num * 7) - num)].AddFront(vertices.Count - 1);
                    colors.Add(curcolor);
                    triangles.Add(vertices.Count - 1);
                }
            }

            verticesNum += num * 7;
        }
        #endregion

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        //StartCoroutine(DrawingAnimation(num));
    }

    private void CreateAnim(int num)
    {
        //curHandPos = hand.transform.position;

        //면생성
        //add vertex
        for (int i = 0; i < num; i++)
        {
            vertices.Add(curCube.transform.InverseTransformPoint(hand.transform.TransformPoint(curVertexOfShape[i])));
            follow.Add(new FollowingVertex(vertices[vertices.Count - 1]));
            colors.Add(thisColor);
        }

        //방향이 바뀌지 않았을때 그 전면 없애기
        if (!waychanged && verticesNum > num * 10)
        {
            triangles.RemoveRange(triangles.Count - num * 2 * 3 - (num - 2) * 3, (num - 2) * 3);
        }
        else if (waychanged)
        {
            waychanged = false;
        }

        //triangle들을 더해준다
        for (int i = 1; i < num - 1; i++)
        {
            if (whichWay == -1 || whichWay == 0)
            {
                triangles.Add(0 + verticesNum);
                triangles.Add(i + verticesNum);
                triangles.Add(i + 1 + verticesNum);
            }
            else
            {
                triangles.Add(i + 1 + verticesNum);
                triangles.Add(i + verticesNum);
                triangles.Add(0 + verticesNum);
            }
        }

        if (verticesNum == 0)
        {
            verticesNum = num;
        }
        else if (verticesNum == num)
        {
            //왼쪽으로 갈때 triangle 바꿔주기
            if (whichWay == -1)
            {
                triangles.Clear();
                //왼쪽으로 가는것임. --------------> 여기서  첫번째 면의 triangle 순서 바꿔주기
                for (int j = 1; j < num - 1; j++)
                {

                    triangles.Add(j + 1);
                    triangles.Add(j);
                    triangles.Add(0);

                }
            }
            for (int i = 0; i < num; i++)
            {
                int a = i, b = (i + 1) % num, c = i + num, d = (i + 1) % num + num;

                if (whichWay == 1)
                {
                    vertices.Add(vertices[a]);
                    follow[a].AddBack(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[c]);
                    follow[c].AddFront(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[b]);
                    follow[b].AddBack(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);



                    vertices.Add(vertices[d]);
                    follow[d].AddFront(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[b]);
                    follow[b].AddBack(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[c]);
                    follow[c].AddFront(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);
                }
                else if (whichWay == -1)
                {
                    vertices.Add(vertices[b]);
                    follow[b].AddBack(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[c]);
                    follow[c].AddFront(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[a]);
                    follow[a].AddBack(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);



                    vertices.Add(vertices[c]);
                    follow[c].AddFront(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[b]);
                    follow[b].AddBack(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[d]);
                    follow[d].AddFront(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);
                }
            }

            verticesNum += num * 7;
        }
        else
        {
            for (int i = verticesNum - num * 7; i < verticesNum - num * 6; i++)
            {
                int a = i, b = (i + 1) % num + verticesNum - num * 7, c = a + num * 7, d = b + num * 7;

                if (whichWay == 1)
                {
                    vertices.Add(vertices[a]);
                    follow[(a / (num * 7) + 1) * num + (a % (num * 7) - num)].AddBack(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[c]);
                    follow[(c / (num * 7) + 1) * num + (c % (num * 7) - num)].AddFront(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[b]);
                    follow[(b / (num * 7) + 1) * num + (b % (num * 7) - num)].AddBack(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);



                    vertices.Add(vertices[d]);
                    follow[(d / (num * 7) + 1) * num + (d % (num * 7) - num)].AddFront(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[b]);
                    follow[(b / (num * 7) + 1) * num + (b % (num * 7) - num)].AddBack(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[c]);
                    follow[(c / (num * 7) + 1) * num + (c % (num * 7) - num)].AddFront(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);
                }
                else if (whichWay == -1)
                {
                    vertices.Add(vertices[b]);
                    follow[(b / (num * 7) + 1) * num + (b % (num * 7) - num)].AddBack(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[c]);
                    follow[(c / (num * 7) + 1) * num + (c % (num * 7) - num)].AddFront(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[a]);
                    follow[(a / (num * 7) + 1) * num + (a % (num * 7) - num)].AddBack(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);


                    vertices.Add(vertices[c]);
                    follow[(c / (num * 7) + 1) * num + (c % (num * 7) - num)].AddFront(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[b]);
                    follow[(b / (num * 7) + 1) * num + (b % (num * 7) - num)].AddBack(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);

                    vertices.Add(vertices[d]);
                    follow[(d / (num * 7) + 1) * num + (d % (num * 7) - num)].AddFront(vertices.Count - 1);
                    colors.Add(thisColor);
                    triangles.Add(vertices.Count - 1);
                }
            }

            verticesNum += num * 7;
        }

        StartCoroutine(DrawingAnimation(num));
    }

    IEnumerator DrawingAnimation(int shapeNum)
    {
        #region drawing
        int tempVertexNum = verticesNum;
        float t = 0;
        float sizebeforechange = size;

        Vector3 middleTemp = new Vector3(0, 0, 0);

        //첫면 생성시
        if (tempVertexNum == shapeNum)
        {
            for (int i = 0; i < shapeNum; i++)
            {
                middleTemp += vertices[i];
            }
        }
        //그 다음부터는
        else
        {
            for (int i = tempVertexNum - 7 * shapeNum; i < tempVertexNum - 6 * shapeNum; i++)
            {
                middleTemp += vertices[i];
            }
        }
        middleTemp = middleTemp / shapeNum;

        List<Vector3> startPos = new List<Vector3>();
        List<Vector3> endPos = new List<Vector3>();

        //첫면 생성시
        if (tempVertexNum == shapeNum)
        {
            for (int i = 0; i < shapeNum; i++)
            {
                Vector3 dir = vertices[i] - middleTemp;
                startPos.Add(dir * changeStartSize + middleTemp);
                endPos.Add(vertices[i]);
            }
        }
        //그 다음 부터는
        else
        {
            for (int i = tempVertexNum - 7 * shapeNum; i < tempVertexNum - 6 * shapeNum; i++)
            {
                Vector3 dir = vertices[i] - middleTemp;
                startPos.Add(dir * changeStartSize + middleTemp);
                endPos.Add(vertices[i]);
            }
        }
        #endregion

        Mesh m = curCube.GetComponent<MeshFilter>().mesh;
        Transform trans = curCube.GetComponent<Transform>();
        MeshCollider mec = curCube.GetComponent<MeshCollider>();
        List<FollowingVertex> f = new List<FollowingVertex>(follow);
        List<Vector3> v = new List<Vector3>(vertices);
        List<Color> c = new List<Color>(colors);
        List<int> tri = new List<int>(triangles);
        bool hasCurCube = true;


        while (t < 1)
        {
            t += changeStartSizeSpeed;

            //그리기를 끝냈으면 vertex 수는 이미 정해져있다 이것을 이용하자
            if (curCube == null && hasCurCube)
            {
                f = new List<FollowingVertex>(follow);
                v = new List<Vector3>(vertices);
                c = new List<Color>(colors);
                tri = new List<int>(triangles);
                for (int i = 0; i < startPos.Count; i++)
                {
                    startPos[i] += trans.InverseTransformVector(dif);
                    endPos[i] += trans.InverseTransformVector(dif);
                }
                hasCurCube = false;
            }

            #region temp
            //curcube가 null이거나 현재 게임오브젝트의 mesh와 m이 같다면 그냥 원래있는 vertices 배열로 계속 업데이트!!!!
            if (hasCurCube)
            {
                #region move
                //첫면 생성시에는
                if (tempVertexNum == shapeNum)
                {
                    for (int i = 0; i < shapeNum; i++)
                    {
                        vertices[i] = Vector3.Lerp(startPos[i], endPos[i], ac.Evaluate(t));
                    }
                }
                // 그 다음부터는
                else
                {
                    for (int i = tempVertexNum - 7 * shapeNum; i < tempVertexNum - 6 * shapeNum; i++)
                    {
                        vertices[i] = Vector3.Lerp(startPos[i - (tempVertexNum - 7 * shapeNum)], endPos[i - (tempVertexNum - 7 * shapeNum)], ac.Evaluate(t));
                        //옆면아이들 따라오게하기
                        for (int j = 0; j < 3; j++)
                        {
                            vertices[f[(i / (shapeNum * 7) + 1) * shapeNum + (i % (shapeNum * 7) - shapeNum)].frontVertex[j]] = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
                        }
                    }

                    //전전 면의 backvertex들을 전전면의 vertex가 움직이는대로 움직이기
                    if (tempVertexNum == shapeNum * 8)
                    {
                        for (int i = 0; i < shapeNum; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                vertices[f[(i / (shapeNum * 7) + 1) * shapeNum + (i % (shapeNum * 7) - shapeNum)].backVertex[j]] = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
                            }
                        }
                    }
                    else
                    {
                        for (int i = tempVertexNum - 14 * shapeNum; i < tempVertexNum - 13 * shapeNum; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                vertices[f[(i / (shapeNum * 7) + 1) * shapeNum + (i % (shapeNum * 7) - shapeNum)].backVertex[j]] = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
                            }
                        }
                    }
                }
                #endregion

                m.Clear();
                m.vertices = vertices.ToArray();
                m.triangles = triangles.ToArray();
                m.colors = colors.ToArray();
                m.RecalculateBounds();
                m.RecalculateNormals();
                mec.sharedMesh = m;
            }
            //만약 그리기를 끝냈으면
            else if (!hasCurCube)
            {
                #region move
                //첫면 생성시에는
                if (tempVertexNum == shapeNum)
                {
                    for (int i = 0; i < shapeNum; i++)
                    {
                        v[i] = Vector3.Lerp(startPos[i], endPos[i], ac.Evaluate(t));
                    }
                }
                // 그 다음부터는
                else
                {
                    v.Clear();
                    v.AddRange(m.vertices);
                    for (int i = tempVertexNum - 7 * shapeNum; i < tempVertexNum - 6 * shapeNum; i++)
                    {
                        v[i] = Vector3.Lerp(startPos[i - (tempVertexNum - 7 * shapeNum)], endPos[i - (tempVertexNum - 7 * shapeNum)], ac.Evaluate(t));
                        //옆면아이들 따라오게하기
                        for (int j = 0; j < 3; j++)
                        {
                            v[f[(i / (shapeNum * 7) + 1) * shapeNum + (i % (shapeNum * 7) - shapeNum)].frontVertex[j]] = new Vector3(v[i].x, v[i].y, v[i].z);
                        }
                    }

                    //전전 면의 backvertex들을 전전면의 vertex가 움직이는대로 움직이기
                    if (tempVertexNum == shapeNum * 8)
                    {
                        for (int i = 0; i < shapeNum; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                v[f[(i / (shapeNum * 7) + 1) * shapeNum + (i % (shapeNum * 7) - shapeNum)].backVertex[j]] = new Vector3(v[i].x, v[i].y, v[i].z);
                            }
                        }
                    }
                    else
                    {
                        for (int i = tempVertexNum - 14 * shapeNum; i < tempVertexNum - 13 * shapeNum; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                v[f[(i / (shapeNum * 7) + 1) * shapeNum + (i % (shapeNum * 7) - shapeNum)].backVertex[j]] = new Vector3(v[i].x, v[i].y, v[i].z);
                            }
                        }
                    }

                }
                #endregion

                //tempVertexNum

                m.Clear();
                m.vertices = v.ToArray();
                m.triangles = tri.ToArray();
                m.colors = c.ToArray();
                m.RecalculateBounds();
                m.RecalculateNormals();
                mec.sharedMesh = m;
            }
            #endregion

            yield return new WaitForEndOfFrame();
        }

        //원래 목표 위치까지 간다음에 size조절이 있었다면 거기까지 움직이게하기
        //curCube인 상태에서만 size조절 가능하니까
        //만약 이면이 마지막 면이고 size가 바뀌었다면
        //각 vertex의 endpos 바꿔주기
        /*if (sizebeforechange != size && verticesNum == tempVertexNum)
        {
            for (int i = tempVertexNum - 7 * shapeNum; i < tempVertexNum - 6 * shapeNum; i++)
            {
                float temptemp = size / sizebeforechange;
                Vector3 dir = vertices[i] - middleTemp;
                endPos[i - (tempVertexNum - 7 * shapeNum)] = middleTemp + dir * temptemp;
            }
            sizebeforechange = size;
        }
        //curCube인 상태에서만 size조절 가능하니까
        //만약 이면이 마지막 면이고 size가 바뀌었다면
        //각 vertex의 endpos 바꿔주기
        if (sizebeforechange != size && verticesNum == tempVertexNum)
        {
            float temptemp = size / sizebeforechange;
            for (int i = 0; i < shapeNum; i++)
            {
                Vector3 dir = vertices[i] - middleTemp;
                endPos[i] = middleTemp + dir * temptemp;
            }
            sizebeforechange = size;
        }*/
    }

    IEnumerator ThumbUp()
    {
        if (tempSize < sizelimitup)
        {
            tempSize += 0.01f;
            float temptemp = tempSize / size;
            Vector3 middleTemp = new Vector3(0, 0, 0);


            //만약 애니메이션 재생 중이 아니라면(원래 있어야할 vertex위치와(middle +dir*size == vertex[1] 이런식으로 확인) 현재 vertex 위치가 같다면) 아래의것 실행 아니면 실행X
            if (verticesNum == curVertexOfShape.Length)
            {
                for (int i = 0; i < curVertexOfShape.Length; i++)
                {
                    middleTemp += vertices[i];
                }

            }
            else
            {
                for (int i = verticesNum - 7 * curVertexOfShape.Length; i < verticesNum - 6 * curVertexOfShape.Length; i++)
                {
                    middleTemp += vertices[i];
                }
            }
            middleTemp = middleTemp / curVertexOfShape.Length;



            if (verticesNum == curVertexOfShape.Length)
            {
                //if (Vector3.Distance(vertices[0], middleTemp) == size)
                {
                    //한면일때라 다른 vertex 안고쳐줘도됨
                    for (int i = 0; i < curVertexOfShape.Length; i++)
                    {
                        Vector3 dir = vertices[i] - middleTemp;
                        vertices[i] = dir * temptemp + middleTemp;
                    }
                    mesh.Clear();
                    mesh.vertices = vertices.ToArray();
                    mesh.triangles = triangles.ToArray();
                    mesh.colors = colors.ToArray();
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();
                }
            }
            else //if(Vector3.Distance(vertices[verticesNum - 7 * curVertexOfShape.Length], middleTemp) == size)
            {
                for (int i = verticesNum - 7 * curVertexOfShape.Length; i < verticesNum - 6 * curVertexOfShape.Length; i++)
                {
                    Vector3 dir = vertices[i] - middleTemp;
                    vertices[i] = dir * temptemp + middleTemp;

                    //붙어있는애들 위치 바꿔주기
                    for (int j = 0; j < 3; j++)
                    {
                        vertices[follow[(i / (curVertexOfShape.Length * 7) + 1) * curVertexOfShape.Length + (i % (curVertexOfShape.Length * 7) - curVertexOfShape.Length)].frontVertex[j]] = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
                    }
                }
                mesh.Clear();
                mesh.vertices = vertices.ToArray();
                mesh.triangles = triangles.ToArray();
                mesh.colors = colors.ToArray();
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
            }

        }

        sizeWaiting = true;
        size = tempSize;

        for (int i = 0; i < locationsOfVertices[curShapeof].Length; i++)
        {
            //정점 사이즈 바꿔주기
            curVertexOfShape[i] = locationsOfVertices[curShapeof][i] * size;
        }
        yield return new WaitForSeconds(0.2f);
        sizeWaiting = false;

    }

    IEnumerator Thumbdown()
    {
        if (sizelimitdown < tempSize)
        {
            tempSize -= 0.01f;
            float temptemp = tempSize / size;
            Vector3 middleTemp = new Vector3(0, 0, 0);
            if (verticesNum == curVertexOfShape.Length)
            {
                for (int i = 0; i < curVertexOfShape.Length; i++)
                {
                    middleTemp += vertices[i];
                }

            }
            else
            {
                for (int i = verticesNum - 7 * curVertexOfShape.Length; i < verticesNum - 6 * curVertexOfShape.Length; i++)
                {
                    middleTemp += vertices[i];
                }
            }
            middleTemp = middleTemp / curVertexOfShape.Length;

            if (verticesNum == curVertexOfShape.Length)
            {
                for (int i = 0; i < curVertexOfShape.Length; i++)
                {
                    Vector3 dir = vertices[i] - middleTemp;
                    vertices[i] = dir * temptemp + middleTemp;
                }
            }
            else
            {
                for (int i = verticesNum - 7 * curVertexOfShape.Length; i < verticesNum - 6 * curVertexOfShape.Length; i++)
                {
                    Vector3 dir = vertices[i] - middleTemp;
                    vertices[i] = dir * temptemp + middleTemp;

                    for (int j = 0; j < 3; j++)
                    {
                        vertices[follow[(i / (curVertexOfShape.Length * 7) + 1) * curVertexOfShape.Length + (i % (curVertexOfShape.Length * 7) - curVertexOfShape.Length)].frontVertex[j]] = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
                    }
                }
            }

            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.colors = colors.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

        }
        sizeWaiting = true;
        size = tempSize;

        for (int i = 0; i < locationsOfVertices[curShapeof].Length; i++)
        {
            //정점 사이즈 바꿔주기
            curVertexOfShape[i] = locationsOfVertices[curShapeof][i] * size;
        }
        yield return new WaitForSeconds(0.2f);
        sizeWaiting = false;

    }


}