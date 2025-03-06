using TMPro;
using UnityEngine;

/// <summary>
/// Sets the input field IP string to the one that is already saved in the system On Start. 
/// If there is no IP saved in the system, keeps the default text.
/// </summary>
public class LoadServerIP : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField _ipInputField;

    // Start is called before the first frame update
    void Start()
    {
        if (_ipInputField == null)
        {
            return;
        }

        string savedIP = PlayerPrefs.GetString(Constants.ServerIPKey, string.Empty);
        if (!string.IsNullOrEmpty(savedIP))
        {
            _ipInputField.text = savedIP;
        }
    }
}
