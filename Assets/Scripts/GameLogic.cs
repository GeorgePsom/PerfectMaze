using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameLogic : MonoBehaviour
{

    public GameObject mainCamera, playerCamera;
    public MazeGenerator mazeGenerator;
    public Canvas uiGenerator;
    public GameObject enemy;
    public GameObject gameOverUI, victoryUI;
    public GameObject player;
    private EnemyControler enemyControler;
    public NavMeshBaker navMeshBaker;
    [HideInInspector]
    public bool useGPU;
    [HideInInspector]
    public bool gameOver, victory;
    private bool play;

    private float playerSpeed;
    private float lookSensitivity;
    [HideInInspector]
    public int nEnemies = 0;
    private float waitTime = 1.5f;

    private GameObject[] enemies;
    // Start is called before the first frame update
    void Start()
    {
        mainCamera.SetActive(true);
        playerCamera.SetActive(false);
        playerSpeed = player.GetComponent<FirstPersonMovement>().speed;
        lookSensitivity = player.transform.GetChild(0).GetComponent<FirstPersonLook>().sensitivity;

    }


    public void ActivatePlayMode()
    {
        // Hide UI
        uiGenerator.enabled = false;
        // Deactivate fly camera
        mainCamera.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        // activate player camera controler and place in the maze's entrance.
        playerCamera.transform.position = new Vector3(mazeGenerator.getMazeEntrance(), 1.0f, -mazeGenerator.solver.graph.padding.y);
        playerCamera.SetActive(true);
        player.GetComponent<FirstPersonMovement>().speed = playerSpeed;
        player.transform.GetChild(0).GetComponent<FirstPersonLook>().sensitivity = lookSensitivity;
        play = true;



        mazeGenerator.GenerateMeshColliders(useGPU);
        navMeshBaker.Bake();


        // Spawn Enemies
        enemies = new GameObject[nEnemies];
        for (int i = 0; i < nEnemies; i++)
        {
            float x = Random.Range(0, mazeGenerator.solver.graph.Width);
            float z = Random.Range(0, mazeGenerator.solver.graph.Height);
            enemies[i] = GameObject.Instantiate(enemy, new Vector3(x, 1.0f, z), Quaternion.Euler(new Vector3(0.0f, 180.0f, 0.0f)));

        }

        gameOver = false;
        gameOverUI.SetActive(false);
        victory = false;
        victoryUI.SetActive(false);
    }

    void DeactivatePlayerMode()
    {
        Cursor.lockState = CursorLockMode.None;
        mainCamera.SetActive(true);
        playerCamera.SetActive(false);
        OnDestroy();
        uiGenerator.enabled = true;
        gameOverUI.SetActive(false);
        victoryUI.SetActive(false);
        play = false;


    }

    private void DestroyEnemies()
    {
        if (enemies != null)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                Destroy(enemies[i]);
            }
            enemies = null;
        }
    }
    private void OnDestroy()
    {
        DestroyEnemies();
    }

    private void GameOver()
    {
        gameOverUI.SetActive(true);
        // disable player's movement and look
        player.GetComponent<FirstPersonMovement>().speed = 0;
        player.transform.GetChild(0).GetComponent<FirstPersonLook>().sensitivity = 0;

    }

    private void Victory()
    {
        victoryUI.SetActive(true);
        // disable player's movement and look
        player.GetComponent<FirstPersonMovement>().speed = 0;
        player.transform.GetChild(0).GetComponent<FirstPersonLook>().sensitivity = 0;
        DestroyEnemies();

    }
    // Update is called once per frame
    void Update()
    {
        if (play)
        {

            if (gameOver)
            {
                GameOver();
                waitTime -= Time.deltaTime;
                // wait 2 seconds just for the enemy's grab animation to finish
                if (waitTime <= 0)
                {
                    gameOver = false;
                    waitTime = 2.0f;
                    DestroyEnemies();

                }

            }
            if (player.transform.position.z > mazeGenerator.solver.graph.Height + mazeGenerator.solver.graph.padding.y * 0.5f && !victory)
            {
                victory = true;
                Victory();
            }
            // in case player falls off the map
            if (player.transform.position.y < -1)
            {
                GameOver();
                
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DeactivatePlayerMode();
            }

        }


    }

}
