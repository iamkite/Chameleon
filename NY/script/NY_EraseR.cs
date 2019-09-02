using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NY_EraseR : MonoBehaviour
{

    private Transform handR;
    public GameObject output;
    public GameObject trashcan;
    public LineRenderer lr;
    public GameObject eraseAll;
    public GameObject nothingToErase;

    private GameObject selectedObj;
    AudioSource sound;
    OVRHapticsClip clip;

    //모드
    public enum EraseMode
    {
        SelectErase,
        EraseAll
    }
    public EraseMode eraseMode;

    // Use this for initialization
    void Start()
    {
        handR = GameManager.Instance.RHand;
        eraseMode = EraseMode.SelectErase;
        sound = GetComponent<AudioSource>();
        clip = new OVRHapticsClip(sound.clip);
    }

    void OnEnable() {
        output.GetComponent<HC_Output>().SetOutlineColor(Color.red);
    }

    void OnDisable() {
        output.GetComponent<HC_Output>().SetOutlineColor(Color.white);
    }

    // Update is called once per frame
    void Update()
    {
        switch (eraseMode)
        {
            case EraseMode.SelectErase: SelectErase(); break;
            case EraseMode.EraseAll: EraseAll(); break;
        }
    }

    private void SelectErase()
    {
        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            Ray ray = new Ray(handR.transform.position, handR.transform.forward);
            RaycastHit hit;

            lr.enabled = true;
            lr.SetPosition(0, GameManager.Instance.RCon.TransformPoint(GameManager.Instance.RlrStart));
            lr.SetPosition(1, handR.transform.position + handR.transform.forward * 3);

            if (Physics.Raycast(ray, out hit, 1000f, 1 << 9))
            {
                //output에 부딪히면 linerenderer활성화
                //lr.enabled = true;
                lr.SetPosition(0, GameManager.Instance.RCon.TransformPoint(GameManager.Instance.RlrStart));
                lr.SetPosition(1, hit.point);
                lr.endColor = Color.white;

                if (selectedObj == null)
                {
                    //선택된 output한개의 outline활성화(무엇을 선택했는지 보여주기 위함)
                    selectedObj = hit.transform.gameObject;
                    selectedObj.GetComponent<Outline>().enabled = true;
                }
                else
                {
                    //다른 output이 선택되었다면
                    if (selectedObj != hit.transform.gameObject)
                    {
                        selectedObj.GetComponent<Outline>().enabled = false;

                        selectedObj = hit.transform.gameObject;
                        selectedObj.GetComponent<Outline>().enabled = true;
                    }
                    //똑같은 애면 아무것도 안하기
                }
            }
            //선택된 output이 있는 상태에서 빈쪽으로 갈때
            else if (selectedObj != null)
            {
                //lr.enabled = false;
                selectedObj.GetComponent<Outline>().enabled = false;
                selectedObj = null;
            }
        }
        //선택된 output이 있는 상태에서 트리거를 떼면 destroy
        else if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            if (selectedObj != null)
            {
                OVRHaptics.RightChannel.Preempt(clip);
                sound.Play();
                selectedObj.transform.parent = trashcan.transform;
                selectedObj.SetActive(false);
                NY_Undo.work.Push(1);
                NY_Undo.deletedobj.Push(new GameObject[] {selectedObj});
                NY_Undo.redo = false;
                //Destroy(selectedObj);
            }
            lr.enabled = false;
            selectedObj = null;
        }
    }

    private void EraseAll()
    {
        //지우기
        if (output.transform.childCount != 0 && eraseAll.activeInHierarchy == false && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            for (int i = 0; i < output.transform.childCount; i++)
            {
                output.transform.GetChild(i).GetComponent<Outline>().enabled = true;
            }
            //정말 다 지우시겠습니까? 안내창 뜨게하기
            eraseAll.SetActive(true);

        }
        //아무것도 없는데 눌렀을때 없는데 안내창뜨게하기
        else if (output.transform.childCount == 0 && nothingToErase.activeInHierarchy == false && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            //지울게 없어요! 안내창
            nothingToErase.SetActive(true);

        }
        else if (eraseAll.activeInHierarchy == true)
        {
            //a누르면 지우기
            //if (OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                //EraseAllConfirm();
            }
            //b누르면 취소
            //else if (OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                //EraseAllCancel();
            }
        }
        else if (nothingToErase.activeInHierarchy == true)
        {
            //A버튼 누르면 안내창 끄기
            if (OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                nothingToErase.SetActive(false);
            }
        }
    }

    public void EraseAllConfirm()
    {
        OVRHaptics.LeftChannel.Preempt(clip);
        OVRHaptics.RightChannel.Preempt(clip);
        sound.Play();
        int outputnum = output.transform.childCount;
        GameObject[] g = new GameObject[outputnum];
        for (int i = 0; i < outputnum; i++)
        {
            g[i] = output.transform.GetChild(0).gameObject;
            output.transform.GetChild(0).gameObject.SetActive(false);
            output.transform.GetChild(0).gameObject.transform.parent = trashcan.transform;
            //Destroy(output.transform.GetChild(i).gameObject);
        }
        NY_Undo.work.Push(1);
        NY_Undo.deletedobj.Push(g);
        NY_Undo.redo = false;
        eraseAll.SetActive(false);
    }

    public void EraseAllCancel()
    {

        for (int i = 0; i < output.transform.childCount; i++)
        {
            output.transform.GetChild(i).GetComponent<Outline>().enabled = false;
        }
        eraseAll.SetActive(false);
    }
}
