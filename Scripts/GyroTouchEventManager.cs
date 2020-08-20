using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using HeliumDreamsTools;

public class GyroTouchEventManager : EventTrigger
{
    public override void OnPointerDown(PointerEventData data)
    {
        //Debug.Log("OnPointerDown " + data.pointerId);
        GyroscopicTouchCamera.instance.SetupNewTouch(data.pointerId);
    }

    public override void OnPointerEnter(PointerEventData data)
    {
        //Debug.Log("OnPointerEnter " + data.pointerId);
    }

    public override void OnPointerExit(PointerEventData data)
    {
        //Debug.Log("OnPointerExit " + data.pointerId);
    }

    public override void OnPointerUp(PointerEventData data)
    {
        //Debug.Log("OnPointerUp " + data.pointerId);
        GyroscopicTouchCamera.instance.EndOldTouch(data.pointerId);
    }
}