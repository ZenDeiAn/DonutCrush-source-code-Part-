using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Airpass.DesignPattern;
using Airpass.Utility;
using Cinemachine;

public class CameraFollower : SingletonUnity<CameraFollower>
{
    [SerializeField] private List<CinemachineVirtualCamera> virtualCameras;
    [SerializeField] private List<Vector3> gamingVCRootPosRot;
    [SerializeField] private Vector3 gameOverVCOffset;
    [SerializeField] private RenderTexture noneStateRenderTexture;

    // Set Gaming State CinemachineVirtualCamera's transform by player index.
    public void SetGamingVirtualCamera(int index)
    {
        Transform vcTransform = virtualCameras[(int)GameState.gaming].transform;
        Vector3 posRot = gamingVCRootPosRot[index];
        Vector3 pos = vcTransform.position;
        pos.x = posRot.x;
        pos.z = posRot.z;
        Vector3 rot = vcTransform.eulerAngles;
        rot.y = posRot.y;
        vcTransform.position = pos;
        vcTransform.eulerAngles = rot;
        vcTransform = virtualCameras[(int)GameState.result].transform;
        rot = vcTransform.eulerAngles;
        rot.y = posRot.y;
        vcTransform.eulerAngles = rot;
    }

    public void RemoveGameOverVCFollowTarget()
    {
        var vc = virtualCameras[(int)GameState.gameOver];
        vc.DestroyCinemachineComponent<Cinemachine3rdPersonFollow>();
    }

    // Set GameOver State CinemachineVirtualCamera's follow and lookat target.
    public void SetGameOverVirtualCamera(Transform target)
    {
        var vc = virtualCameras[(int)GameState.gameOver];
        var c3pf = vc.AddCinemachineComponent<Cinemachine3rdPersonFollow>();
        c3pf.ShoulderOffset = gameOverVCOffset;
        vc.Follow = vc.LookAt = target;
    }

    // Get CinemachineVirtualCamera by GameState
    public CinemachineVirtualCamera GetVirtualCamera(GameState state)
    {
        return virtualCameras[(int)state];
    }

    public Transform Target
    {
        set => virtualCameras.ForEach(t =>
        {
            if (t != null)
            {
                t.LookAt = t.Follow = value;
            }
        });
    }

    public void SetCameraByGameState(string gameState)
    {
        if (Enum.TryParse(gameState, out GameState state))
        {
            SetVirtualCamera((int)state);
        }
    }

    public void SetCameraByGameState(GameState state)
    {
        SetVirtualCamera((int)state);
    }

    IEnumerator DieVirtualCameraProcessCoroutine()
    {
        yield return Utility.GetWaitForSecond(1.0f);
    }

    private void SetVirtualCamera(int index)
    {
        if (virtualCameras.Count > index)
        {
            foreach (var vc in 
                     virtualCameras.Where(vc => vc.gameObject.activeSelf))
            {
                vc.gameObject.SetActive(false);
            }
            virtualCameras[index].gameObject.SetActive(true);
        }
    }
}
