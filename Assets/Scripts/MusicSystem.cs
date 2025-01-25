using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MusicSystem : MonoBehaviour
{
    [SerializeField] private AudioClip[] allSongs;
    [SerializeField] private AudioSource audioSource;
    
    private List<AudioClip> remainingSongs;
    private bool isInitialized = false;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        InitializeSongList();
        PlayNextSong();
    }

    private void Update()
    {
        // Check if the current song has finished playing
        if (!audioSource.isPlaying && isInitialized)
        {
            PlayNextSong();
        }
    }

    private void InitializeSongList()
    {
        // Create a new list of remaining songs
        remainingSongs = new List<AudioClip>(allSongs);
        isInitialized = true;
    }

    private void PlayNextSong()
    {
        // If all songs have been played, refill the list
        if (remainingSongs.Count == 0)
        {
            InitializeSongList();
        }

        // Choose a random song from the remaining songs
        int randomIndex = Random.Range(0, remainingSongs.Count);
        AudioClip songToPlay = remainingSongs[randomIndex];

        // Remove the chosen song from the remaining songs list
        remainingSongs.RemoveAt(randomIndex);

        // Play the chosen song
        audioSource.clip = songToPlay;
        audioSource.Play();

        // Optional: Log the currently playing song
        Debug.Log($"Now playing: {songToPlay.name}");
    }

    // Public method to manually skip to the next song
    public void SkipCurrentSong()
    {
        PlayNextSong();
    }

    // Public method to get the name of the currently playing song
    public string GetCurrentSongName()
    {
        return audioSource.clip != null ? audioSource.clip.name : "No song playing";
    }
}