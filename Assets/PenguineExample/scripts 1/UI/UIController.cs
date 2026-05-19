using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public Button Agent1Button;
    public Button Agent2Button;
    public Button Agent4Button;
    public Button Agent6Button;
    public Button Agent8Button;

    public Button Env1Button;
    public Button Env2Button;
    public Button Env3Button;
    public Button Env4Button;

    void Start()
    {
        
    }

    void Update()
    {
    }

    public void LoadScene(string scene_name)
    {
        SceneManager.LoadScene(scene_name);
    }
}
