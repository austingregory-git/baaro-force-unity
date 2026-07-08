using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonBounceTrigger : MonoBehaviour, IPointerEnterHandler
{
    private Animator animator;
    private AudioSource audioSource;

    void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (animator != null)
            animator.SetTrigger("Hover");
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }
}