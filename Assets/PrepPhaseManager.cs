using UnityEngine;
using UnityEngine.SceneManagement;

public class PrepPhaseManager : MonoBehaviour
{
    
    public void SkipPrepPhase()
    {
        Debug.Log("Clicked");
        SceneManager.LoadScene("CharacterSelection");
    }
}
