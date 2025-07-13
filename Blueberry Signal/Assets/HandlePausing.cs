using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandlePausing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pauseScreen;

    private bool gamePaused = false;

    #region Input Handling

    public void PauseInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            TogglePause();
            return;
        }
    }

    #endregion

    private void TogglePause()
    {
        PauseGame(!gamePaused);
    }

    private void PauseGame(bool b)
    {
        gamePaused = b;
        pauseScreen.SetActive(b);

        if (b)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
}
