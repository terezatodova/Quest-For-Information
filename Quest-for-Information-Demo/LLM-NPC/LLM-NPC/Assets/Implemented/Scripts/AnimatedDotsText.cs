using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AnimatedDotsText : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _tmp;

    private Coroutine _dotCoroutine;


    private void OnEnable()
    {
        _dotCoroutine = StartCoroutine(AnimateDots());
    }

    private void OnDisable()
    {
        if (_dotCoroutine != null)
        {
            StopCoroutine(_dotCoroutine);
            _dotCoroutine = null;
        }
        _tmp.text = string.Empty;
    }

    private IEnumerator AnimateDots()
    {
        int dotCount = 0;

        while (true)
        {
            dotCount = (dotCount % 3) + 1;
            _tmp.text = new string('.', dotCount).Replace(".", ". ");

            yield return new WaitForSecondsRealtime(0.5f);
        }
    }
}
