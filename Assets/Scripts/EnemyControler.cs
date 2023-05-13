using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyControler : MonoBehaviour
{


   

   
    [HideInInspector]
    public LayerMask playerMask;
    [HideInInspector]
    public LayerMask obstacleMask;
    public GameObject light;
    public GameLogic gamelogic;
  


    public float speedWalk = 2;
    public float fastSpeedWalk = 3;
    public float speedRun = 9;
    public float viewRadius = 15;
    public float viewAngle = 90;

    bool isPatrol;
    bool caughtPlayer;
    bool isSearching;

    public float startChasingTime = 5.0f;
    private float chasingTime;

    public float startSearchingTime = 5.0f;
    private float searchingTime;

    private Vector3 lastPlayerKnownPosition;
    [HideInInspector]
    private NavMeshAgent navMeshAgent;
    private GameObject player;
    private FirstPersonAudio fpAudio;
    private Animation anim;
    private bool isInitialized = false;
    private int width, height;
    [HideInInspector]
    public MazeGenerator maze;

    bool hasReachedDestination = true;


    public void Awake()
    {
        
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.speed = speedWalk;
        player = GameObject.FindGameObjectWithTag("Player");
        fpAudio = GameObject.FindGameObjectWithTag("AudioPlayer").GetComponent<FirstPersonAudio>();
        maze = GameObject.FindGameObjectWithTag("Maze").GetComponent<MazeGenerator>();
        gamelogic = GameObject.Find("GameLogic").GetComponent<GameLogic>();
        playerMask = LayerMask.NameToLayer("Player");
        obstacleMask = LayerMask.NameToLayer("Obstacle");
        width = maze.solver.graph.Width;
        height = maze.solver.graph.Height;
        // Set the stoppingDistance to padding value in case a random destination is inside a wall and the agent get stuck because it cant reach the destination.
        navMeshAgent.stoppingDistance = 1.0f;
        anim = GetComponent<Animation>();
        isPatrol = true;
        caughtPlayer = false;
        isSearching = false;
        chasingTime = startChasingTime;
        searchingTime = startSearchingTime;
        isInitialized = true;
        light.SetActive(false);
        
    }

   
    void Update()
    {
        if(isInitialized)
        {
            if(!caughtPlayer)
            {
                if (isPatrol)
                {
                    Patrolling();
                }
                else
                {
                    Chasing();
                }
            }
            else
            {
                Caught();
            }
        }
        
    }

    private void Patrolling()
    {
        anim.Play("walk");
        if (hasReachedDestination)
        {
            Vector2 randomDestination = new Vector2(Random.Range(0, width), Random.Range(0, height));
            navMeshAgent.SetDestination(new Vector3(randomDestination.x, 1.0f, randomDestination.y));
            hasReachedDestination = false;
        }

        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            hasReachedDestination = true;
        }

        CheckForPlayer();
        
    }



    private void Chasing()
    {
        
        bool playerSeen = false;
        Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle * 0.5f)
        {
            float dstToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (!Physics.Raycast(transform.position, dirToPlayer, dstToPlayer, obstacleMask))
            {
                if (Vector3.Distance(transform.position, player.transform.position) < viewRadius)
                {
                    playerSeen = true;
                    if (!light.activeSelf)
                        light.SetActive(true);
                    chasingTime = startChasingTime;
                    navMeshAgent.SetDestination(player.transform.position);
                    anim.Play("run");
                }
                
            }
        }
        bool isChasing = false;
        // Player is not directly seen but enemy still chasing them for "startChasingTime" seconds
        if(!playerSeen && !isSearching)
        {
            isChasing = true;
            chasingTime -= Time.deltaTime;
            navMeshAgent.SetDestination(player.transform.position);
            anim.Play("run");
            if (chasingTime <=0 && !isSearching)
            {
                chasingTime = startChasingTime;
                InitSearch();
                isChasing = false;
            }
        }

        // Enter search mode, enemy is going in a random area close to the player's last known position.
        if(isSearching && !playerSeen)
        {
            searchingTime -= Time.deltaTime;
            anim.Play("walk");
            if (light.activeSelf)
                light.SetActive(false);
            if(hasReachedDestination && searchingTime > 0)
            {
                hasReachedDestination = false;
                Vector2 randomDestination = new Vector2(Random.Range(lastPlayerKnownPosition.x - 10, lastPlayerKnownPosition.x + 10),
                    Random.Range(lastPlayerKnownPosition.z - 10, lastPlayerKnownPosition.z + 10));
                navMeshAgent.SetDestination(new Vector3(randomDestination.x, 1.0f, randomDestination.y));



            }
            // Go back to patrol mode
            if (searchingTime <= 0)
            {
                isPatrol = true;
                isSearching = false;
                searchingTime = startSearchingTime;
            }
        }

        
        // player is caught
        if (Vector3.Distance(transform.position, player.transform.position) < 1.0f && (playerSeen || isChasing) )
        {
            caughtPlayer = true;
            anim.Play("grab");           

        }

    }

    private void Caught()
    {

        gamelogic.gameOver = true;
    }

    private void OnAnimatorMove()
    {
        
    }


   
    void InitChase()
    {
        navMeshAgent.speed = speedRun;
        fpAudio.playJumpscare = true;
    }
    
    void InitSearch()
    {
        navMeshAgent.speed = fastSpeedWalk;
        lastPlayerKnownPosition = player.transform.position;
        navMeshAgent.SetDestination(lastPlayerKnownPosition);
        hasReachedDestination = false;
        isSearching = true;
        searchingTime = startChasingTime;
    }

    void CaughtPlayer()
    {
        caughtPlayer = true;
    }
    
   

    void CheckForPlayer()
    {
        // if player is in the view radius
        Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
        if(Vector3.Angle(transform.forward, dirToPlayer) < viewAngle * 0.5f)
        {   
            float dstToPlayer = Vector3.Distance(transform.position, player.transform.position);
            // no obstacle is between them
            if(!Physics.Raycast(transform.position, dirToPlayer, dstToPlayer,obstacleMask))
            {
                // player is in view range
                if (Vector3.Distance(transform.position, player.transform.position) < viewRadius)
                {
                    isPatrol = false;
                    light.SetActive(true);
                    InitChase();

                }
            }
        }
        
    }
}
