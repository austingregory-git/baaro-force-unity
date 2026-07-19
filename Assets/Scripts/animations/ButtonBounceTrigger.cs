using UnityEngine;
using UnityEngine.EventSystems;

namespace BaaroForce.Animations
{
    public class ButtonBounceTrigger : MonoBehaviour, IPointerEnterHandler
    {
        private Animator _animator;
        private AudioSource _audioSource;

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _audioSource = GetComponent<AudioSource>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_animator != null)
                _animator.SetTrigger("Hover");
            if (_audioSource != null && _audioSource.clip != null)
            {
                _audioSource.Play();
            }
        }
    }
}
