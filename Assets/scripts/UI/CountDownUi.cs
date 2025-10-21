using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CountDownUi : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI text;
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip clipEnd;
    [SerializeField] AudioClip clipCount;

    public UnityEvent EndCountDownEvent { get; } = new UnityEvent();

    private void SetCount(int value)
    {
        switch (value)
        {
            case 0:
                text.text = "";
                break;
            default:
                text.text = value.ToString();
                break;
        }
        if (value <= 3)
            source.PlayOneShot(value == 0 ? clipEnd : clipCount);

    }
    public IEnumerator CountDown(int countdown)
    {
        gameObject.SetActive(true);

        while (countdown >= 0)
        {
            SetCount(countdown);
            yield return new WaitForSeconds(1f);
            countdown--;
        }
        gameObject.SetActive(false);
        EndCountDownEvent.Invoke();
    }
}
