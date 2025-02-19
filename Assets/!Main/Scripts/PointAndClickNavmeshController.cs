using UnityEngine;
using UnityEngine.AI;

public class PointAndClickNavmeshController : MonoBehaviour
{
    [Header("NavMesh Settings")]
    public NavMeshAgent agent;

    [Header("Layer Settings")]
    public LayerMask groundLayer;

    private Camera mainCamera;
    private bool isMoving = false;
    private Vector3 targetPoint;
    private NavMeshPath debugPath;

    private void Start()
    {
        // Get the main camera
        mainCamera = Camera.main;

        // Ensure agent is assigned
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        agent.isStopped = true;
        agent.updateRotation = false;

        debugPath = new NavMeshPath();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            MoveToClickPoint();
        }

        if (isMoving && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StopMovement();
        }

        if (isMoving)
        {
            SmoothRotateTowards(agent.steeringTarget);
        }
    }

    private void MoveToClickPoint()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.point);
            isMoving = true;
            targetPoint = hit.point;

            // Calculate path for debug drawing
            agent.CalculatePath(hit.point, debugPath);
        }
    }

    private void SmoothRotateTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;

        if (direction.magnitude > 0.1f) // Avoid jittering when stopping
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    private void StopMovement()
    {
        isMoving = false;
        agent.isStopped = true;
    }

    private void OnDrawGizmos()
    {
        if (targetPoint != Vector3.zero)
        {
            // Draw a red sphere at the clicked position
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(targetPoint, 0.2f);
        }

        if (agent != null && isMoving)
        {
            // Draw a blue line from the agent to the next steering target
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, agent.steeringTarget);

            // Draw the calculated path as a green line
            if (debugPath != null && debugPath.corners.Length > 1)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < debugPath.corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(debugPath.corners[i], debugPath.corners[i + 1]);
                }
            }
        }
    }
}
