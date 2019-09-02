using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NY_Undo : MonoBehaviour {

    AudioSource[] sound;

    static public bool redo = false;
    public GameObject trashcan;
    public GameObject output;
    static public LimitedStack<int> work = new LimitedStack<int>(30);
    LimitedStack<int> cancel = new LimitedStack<int>(30);

    static public LimitedStack<GameObject> createdobj = new LimitedStack<GameObject>(15);
    LimitedStack<GameObject> zzcreatedobj = new LimitedStack<GameObject>(15);

    static public LimitedStack<GameObject[]> deletedobj = new LimitedStack<GameObject[]>(15);
    LimitedStack<GameObject[]> zzdeletedobj = new LimitedStack<GameObject[]>(15);

    static public LimitedStack<Transformstruct[]> transformobj = new LimitedStack<Transformstruct[]>(15);
    LimitedStack<Transformstruct[]> zztransformobj = new LimitedStack<Transformstruct[]>(15);


    static public LimitedStack<ColormeshStruct> colorchanged = new LimitedStack<ColormeshStruct>(30);
    LimitedStack<ColormeshStruct> colorbefore = new LimitedStack<ColormeshStruct>(30);

    static public LimitedStack<ColormeshStruct> modifyobj = new LimitedStack<ColormeshStruct>(15);
    LimitedStack<ColormeshStruct> zzmodifyobj = new LimitedStack<ColormeshStruct>(15);

    // Use this for initialization
    void Start () {
        sound = GetComponents<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
        //undo
        if ((OVRInput.Get(OVRInput.Button.PrimaryHandTrigger,OVRInput.Controller.LTouch) && OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickLeft, OVRInput.Controller.LTouch) ||
            (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) && OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickLeft, OVRInput.Controller.RTouch)))
            && work.Count>0)
        {
            sound[0].Play();
            int pop = work.Pop();
            switch (pop)
            {
                case 0:Undocreateobj(); break;
                case 1: UndoDelete(); break;
                case 2: UndoTransform(); break;
                case 3: UndoColor(); break;
                case 4: UndoModify(); break;
                default:; break;
            }
            redo = true;
        }
        //redo : redo는 취소를 취소한다. 즉 마지막으로 한 행동이 취소일때만 redo가 가능
        else if (redo && (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) && OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickRight, OVRInput.Controller.LTouch) ||
            (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) && OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickRight, OVRInput.Controller.RTouch))) 
            && cancel.Count>0)
        {
            sound[1].Play();
            int pop = cancel.Pop();
            switch (pop)
            {
                case 0: Redocreateboj(); break;
                case 1: RedoDelete(); break;
                case 2: RedoTransform(); break;
                case 3: RedoColor(); break;
                case 4: RedoModify(); break;
                default:; break;
            }
            redo = true;
        }

        
        if(cancel.Count > 0 && !redo)
        {
            cancel.Clear();
        }

	}

    //만든거 취소
    void Undocreateobj()
    {
        GameObject g = createdobj.Pop();
        g.SetActive(false);
        g.transform.parent = trashcan.transform;
        cancel.Push(0);
        zzcreatedobj.Push(g);
    }

    //만든거 취소하는걸 취소 => 다시 복귀
    void Redocreateboj()
    {
        GameObject g = zzcreatedobj.Pop();
        g.SetActive(true);
        g.transform.parent = output.transform;
        work.Push(0);
        createdobj.Push(g);
    }

    //삭제 취소
    void UndoDelete()
    {
        GameObject[] g = deletedobj.Pop();
        for(int i = 0; i < g.Length; i++)
        {
            g[i].SetActive(true);
            g[i].transform.parent = output.transform;
        }
        cancel.Push(1);
        zzdeletedobj.Push(g);
    }

    //삭제 취소를 취소
    void RedoDelete()
    {
        GameObject[] g = zzdeletedobj.Pop();
        for (int i = 0; i < g.Length; i++)
        {
            g[i].SetActive(false);
            g[i].transform.parent = trashcan.transform;
        }
        work.Push(1);
        deletedobj.Push(g);
    }


    void UndoTransform()
    {
        Transformstruct[] t = transformobj.Pop();
        for (int i = 0; i<t.Length; i++)
        {
            t[i].g.transform.position = t[i].beforetrans.pos;
            t[i].g.transform.rotation = t[i].beforetrans.rot;
            t[i].g.transform.localScale = t[i].beforetrans.scale;
        }
        cancel.Push(2);
        zztransformobj.Push(t);
    }

    void RedoTransform()
    {
        Transformstruct[] t = zztransformobj.Pop();
        for(int i = 0; i<t.Length; i++)
        {
            t[i].g.transform.position = t[i].aftertrans.pos;
            t[i].g.transform.rotation = t[i].aftertrans.rot;
            t[i].g.transform.localScale = t[i].aftertrans.scale;
        }
        work.Push(2);
        transformobj.Push(t);
    }


    void UndoColor()
    {
        ColormeshStruct c = colorchanged.Pop();
        c.g.GetComponent<MeshFilter>().mesh = c.beforemesh;
        cancel.Push(3);
        colorbefore.Push(c);
    }

    void RedoColor()
    {
        ColormeshStruct c = colorbefore.Pop();
        c.g.GetComponent<MeshFilter>().mesh = c.aftermesh;
        work.Push(3);
        colorchanged.Push(c);
    }


    void UndoModify()
    {
        ColormeshStruct c = modifyobj.Pop();
        c.g.GetComponent<MeshFilter>().mesh = c.beforemesh;
        cancel.Push(4);
        zzmodifyobj.Push(c);
    }

    void RedoModify()
    {
        ColormeshStruct c = zzmodifyobj.Pop();
        c.g.GetComponent<MeshFilter>().mesh = c.aftermesh;
        work.Push(4);
        modifyobj.Push(c);
    }
}

public class LimitedStack<T>
{
    List<T> list = new List<T>();
    int capacity;

    public int Count
    {
        get { return this.list.Count; }
    }

    public LimitedStack(int capacity)
    {
        this.capacity = capacity;
    }

    internal T Pop()
    {
        var t = this.list[0];
        this.list.RemoveAt(0);
        return t;
    }

    internal void Push(T x)
    {
        this.list.Insert(0, x);
        if (this.list.Count > capacity)
        {
            this.list.RemoveAt(this.list.Count - 1);
        }
    }

    internal T Peek()
    {
        return this.list[0];
    }

    internal void Clear()
    {
        this.list.Clear();
    }
}

public struct ColormeshStruct
{
    public GameObject g;
    public Mesh beforemesh;
    public Mesh aftermesh;

    public ColormeshStruct(GameObject g, Mesh beforemesh, Mesh aftermesh)
    {
        this.beforemesh = new Mesh();
        this.aftermesh = new Mesh();
        this.g = g;
        this.beforemesh.vertices = beforemesh.vertices;
        this.beforemesh.triangles = beforemesh.triangles;
        this.beforemesh.colors = beforemesh.colors;
        this.beforemesh.RecalculateBounds();
        this.beforemesh.RecalculateNormals();

        this.aftermesh.vertices = aftermesh.vertices;
        this.aftermesh.triangles = aftermesh.triangles;
        this.aftermesh.colors = aftermesh.colors;
        this.aftermesh.RecalculateBounds();
        this.aftermesh.RecalculateNormals();
    }
}

public struct Transformstruct
{
    public GameObject g;
    public newTrans beforetrans;
    public newTrans aftertrans;

    public Transformstruct(GameObject g, newTrans beforetrans, newTrans aftertrans)
    {
        this.beforetrans = new newTrans();
        this.aftertrans = new newTrans();
        this.g = g;
        this.beforetrans.pos = beforetrans.pos;
        this.beforetrans.rot = beforetrans.rot;
        this.beforetrans.scale = beforetrans.scale;
        this.aftertrans.pos = aftertrans.pos;
        this.aftertrans.rot = aftertrans.rot;
        this.aftertrans.scale = aftertrans.scale;
    }
}

public struct newTrans
{
    public GameObject g;
    public Vector3 pos;
    public Quaternion rot;
    public Vector3 scale;

    public newTrans(Transform tf)
    {
        this.g = tf.gameObject;
        this.pos = tf.position;
        this.rot = tf.rotation;
        this.scale = tf.localScale;
    }
}