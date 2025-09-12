using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.N3DS;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
