using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    #region Tunables

    [SerializeField]
    private bool reportSpeedInMPH = false;
    [SerializeField]
    private UnityEngine.UI.Text speedometerLabelText = null;
    [SerializeField]
    private UnityEngine.UI.Text speedometerText = null;

    #endregion

    #region Fields

    private Canvas canvas = null;

    #endregion

    #region Constants

    private const float MPS_TO_MPH = 2.23694f;

    #endregion

    #region Properties

    public ChaseCamera Camera
    {
        set
        {
            if (canvas != null)
            {
                canvas.worldCamera = value.Camera;
                canvas.planeDistance = 0.5f;
            }
        }
    }

    [System.NonSerialized]
    public PlayerController Player = null;

    #endregion

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
    }

    private void Update()
    {
        if (canvas == null || Player == null)
        {
            return;
        }

        if (reportSpeedInMPH)
        {
            speedometerLabelText.text = "MPH";
            speedometerText.text = Mathf.RoundToInt(Player.Dynamics.Speed * MPS_TO_MPH).ToString();
        }
        else
        {
            speedometerLabelText.text = "M/S";
            speedometerText.text = Mathf.RoundToInt(Player.Dynamics.Speed).ToString();
        }
    }
}
