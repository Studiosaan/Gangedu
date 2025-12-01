using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    [SerializeField]
    private Button startButton;

    public Button StartButton
    {
        get
        {
            return startButton;
        }
    }
}
