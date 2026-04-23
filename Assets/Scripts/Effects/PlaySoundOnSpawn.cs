using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlaySoundOnSpawn : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [SerializeField] [Min(0f)] private float volume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, volume);
    }
}
