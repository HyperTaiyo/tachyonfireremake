using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WormEnemy : BaseEnemy
{
    [System.Serializable]
    public class WormSegment
    {   
        public Transform segmentTransform;
        public List<WormEye> eyes;
        public bool isDestroyed;
        [HideInInspector] public Vector3 previousPosition;
        [HideInInspector] public Vector3 targetPosition;
        [HideInInspector] public float followDelay;
        [HideInInspector] public Queue<TimestampedPosition> positionHistory;
    }

    [System.Serializable]
    public class WormEye
    {
        public Transform eyeTransform;
        public float health = 50f;
        public bool isDestroyed;
        public GameObject destroyedEffect;
    }

    [System.Serializable]
    public class TimestampedPosition
    {
        public Vector3 position;
        public float timestamp;

        public TimestampedPosition(Vector3 pos, float time)
        {
            position = pos;
            timestamp = time;
        }
    }

    [Header("Segment Settings")]
    [SerializeField] private float segmentHeightOffset = 0.5f; // Added for better ground align

    [Header("Worm Settings")]
    [SerializeField] private float terrainCheckOffset = 2f;
    [SerializeField] private float maxTerrainAngle = 45f;
    [SerializeField] private float groundCheckDistance = 100f;
    [SerializeField] private List<WormSegment> segments;
    [SerializeField] private float segmentSpacing = 2f;
    [SerializeField] private float minBurrowDepth = 5f;
    [SerializeField] private float maxBurrowDepth = 15f;
    [SerializeField] private float emergeSpeed = 15f;
    [SerializeField] private float burrowSpeed = 8f;
    [SerializeField] private float surfaceDuration = 3f;
    [SerializeField] private float emergeCooldown = 2f;
    [SerializeField] private float baseSegmentDelay = 0.1f;
    [SerializeField] private float segmentFollowSpeed = 10f;
    [SerializeField] private float predictionMultiplier = 1.5f;
    [SerializeField] private float maxJumpHeight = 10f;
    [SerializeField] private float minSegmentDistance = 1f;
    [SerializeField] private float burrowCheckRadius = 1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask segmentLayer;
    [SerializeField] private GameObject burrowEffect;
    [SerializeField] private GameObject emergeEffect;
    [SerializeField] private AudioClip burrowSound;
    [SerializeField] private AudioClip emergeSound;
    [SerializeField] private AudioClip eyeDestroySound;

    private bool isBurrowed = true;
    private bool isEmerging = false;
    private Vector3 burrowedPosition;
    private float nextBurrowTime;
    private Vector3 startPosition;
    private const int positionHistoryLimit = 100;
    private List<SphereCollider> segmentColliders = new List<SphereCollider>();

    [Header("Movement Tweaks")]
    [SerializeField] private float segmentSmoothSpeed = 5f;
    [SerializeField] private float distanceThreshold = 0.1f;
    [SerializeField] private float verticalOffset = 2f;
    [SerializeField] private float burrowFailsafeTime = 5f;

    private Vector3 previousHeadPosition;
    private float burrowStartTime;
    private bool isMoving = false;

    [SerializeField] private float maxAttackDistance = 30f;
    [SerializeField] private float minAttackDistance = 5f;

    protected override void Start()
    {
        base.Start();
        InitializeSegments();
        
        burrowedPosition = transform.position - Vector3.up * minBurrowDepth;
        transform.position = burrowedPosition;
        previousHeadPosition = transform.position;
        
        foreach (var segment in segments)
        {
            if (segment.segmentTransform != null)
            {
                segment.targetPosition = segment.segmentTransform.position - Vector3.up * minBurrowDepth;
                segment.segmentTransform.position = segment.targetPosition;
                segment.previousPosition = segment.targetPosition;
            }
        }

        nextBurrowTime = Time.time;
    }

    protected override void Update()
    {
        if (player == null) return;

        if (Vector3.Distance(transform.position, previousHeadPosition) > distanceThreshold)
        {
            isMoving = true;
            RecordPosition(transform.position);
            previousHeadPosition = transform.position;
        }
        else
        {
            isMoving = false;
        }

        if (isBurrowed && !isEmerging && Time.time > nextBurrowTime)
        {
            TryAttackPlayer();
        }
        
        UpdateSegments();
        EnforceMinimumSegmentDistances();

        if (isEmerging && Time.time - burrowStartTime > burrowFailsafeTime)
        {
            StopAllCoroutines();
            ForceReset();
        }
    }

    private void TryAttackPlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= maxAttackDistance && distanceToPlayer >= minAttackDistance)
        {
            Vector3 predictedPlayerPos = PredictPlayerPosition();
            if (CanBurrow(predictedPlayerPos))
            {
                StartCoroutine(EmergeAtPosition(predictedPlayerPos));
            }
            else
            {
                if (CanBurrow(player.position))
                {
                    StartCoroutine(EmergeAtPosition(player.position));
                }
                else
                {
                    nextBurrowTime = Time.time + 0.5f;
                }
            }
        }
        else
        {
            burrowedPosition = transform.position - Vector3.up * minBurrowDepth;
            nextBurrowTime = Time.time + 0.5f;
        }
    }

    private Vector3 PredictPlayerPosition()
    {
        Vector3 predictedPos = player.position;
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        
        if (playerRb != null)
        {
            predictedPos += playerRb.velocity * (predictionMultiplier * 0.5f);
            
            float playerHeight = player.position.y - GetGroundHeight(player.position);
            float emergeHeight = Mathf.Min(playerHeight * 1.2f, maxJumpHeight);
            predictedPos.y += emergeHeight;

            if (Vector3.Distance(transform.position, predictedPos) > maxAttackDistance)
            {
                Vector3 directionToPlayer = (predictedPos - transform.position).normalized;
                predictedPos = transform.position + directionToPlayer * maxAttackDistance;
            }
        }
        
        return predictedPos;
    }

    private IEnumerator EmergeAtPosition(Vector3 targetPos)
    {
        isEmerging = true;
        burrowStartTime = Time.time;
        
        // Get ground height at target position
        RaycastHit groundHit;
        float groundHeight = targetPos.y;
        if (Physics.Raycast(targetPos + Vector3.up * groundCheckDistance, Vector3.down, out groundHit, groundCheckDistance, groundLayer))
        {
            groundHeight = groundHit.point.y;
            targetPos.y = groundHeight + minBurrowDepth;
        }
        
        float targetBurrowDepth = Mathf.Clamp(
            targetPos.y - groundHeight,
            minBurrowDepth,
            maxBurrowDepth
        );
        
        Vector3 burrowTarget = new Vector3(targetPos.x, groundHeight - targetBurrowDepth, targetPos.z);
        
        yield return StartCoroutine(MoveToPosition(transform.position, burrowTarget, burrowSpeed));
        
        if (emergeSound) audioSource.PlayOneShot(emergeSound);
        if (emergeEffect) Instantiate(emergeEffect, transform.position + Vector3.up, Quaternion.identity);
        
        // Emerge with arc motion
        float moveToSurfaceDuration = Vector3.Distance(transform.position, targetPos) / emergeSpeed;
        float elapsedTime = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(targetPos.x, groundHeight, targetPos.z);
        
        while (elapsedTime < moveToSurfaceDuration)
        {
            float t = elapsedTime / moveToSurfaceDuration;
            float height = Mathf.Sin(t * Mathf.PI) * Mathf.Min(maxJumpHeight, (player.position.y - groundHeight) * 1.5f);
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos.y += height;
            
            RaycastHit hit;
            if (Physics.Raycast(currentPos + Vector3.up, Vector3.down, out hit, groundCheckDistance, groundLayer))
            {
                currentPos.y = Mathf.Max(currentPos.y, hit.point.y + 0.5f);
            }
            
            transform.position = currentPos;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.position = endPos;
        isBurrowed = false;
        isEmerging = false;
        
        yield return new WaitForSeconds(surfaceDuration);
        
        if (burrowSound) audioSource.PlayOneShot(burrowSound);
        if (burrowEffect) Instantiate(burrowEffect, transform.position, Quaternion.identity);
        
        burrowedPosition = transform.position - Vector3.up * minBurrowDepth;
        yield return StartCoroutine(MoveToPosition(transform.position, burrowedPosition, burrowSpeed));
        
        isBurrowed = true;
        nextBurrowTime = Time.time + emergeCooldown;
    }

    private void InitializeSegments()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            if (segment.segmentTransform != null)
            {
                SphereCollider collider = segment.segmentTransform.gameObject.AddComponent<SphereCollider>();
                collider.radius = segmentSpacing * 0.6f;
                collider.isTrigger = true;
                segment.segmentTransform.gameObject.layer = LayerMask.NameToLayer("EnemySegment");
                segmentColliders.Add(collider);

                segment.positionHistory = new Queue<TimestampedPosition>();
                segment.followDelay = baseSegmentDelay * (i + 1);
                
                float spacing = segmentSpacing * (i + 1);
                segment.targetPosition = transform.position - transform.forward * spacing;
                segment.segmentTransform.position = segment.targetPosition;
                segment.previousPosition = segment.targetPosition;
            }
        }
    }

    private bool CanBurrow(Vector3 position)
    {
        RaycastHit hit;
        Vector3 checkStart = position + Vector3.up * terrainCheckOffset;
        
        if (!Physics.Raycast(checkStart, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            return false;
        }
        
        float terrainAngle = Vector3.Angle(hit.normal, Vector3.up);
        if (terrainAngle > maxTerrainAngle)
        {
            return false;
        }
        
        Collider[] obstacles = Physics.OverlapSphere(position, burrowCheckRadius, groundLayer);
        if (obstacles.Length > 0)
        {
            return false;
        }
        
        return true;
    }

    private void ForceReset()
    {
        isEmerging = false;
        isBurrowed = true;
        nextBurrowTime = Time.time + emergeCooldown;
        transform.position = burrowedPosition;
        
        foreach (var segment in segments)
        {
            if (segment.segmentTransform != null)
            {
                segment.targetPosition = burrowedPosition - transform.forward * segmentSpacing;
                segment.segmentTransform.position = segment.targetPosition;
            }
        }
    }

    private void UpdateSegments()
    {
        Vector3 previousSegmentPos = transform.position;
        
        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            if (!segment.isDestroyed && segment.segmentTransform != null)
            {
                Vector3 targetPos = GetDelayedPosition(segment);
                
                // Add vertical offset and ground alignment
                RaycastHit hit;
                if (Physics.Raycast(targetPos + Vector3.up * 2f, Vector3.down, out hit, 3f, groundLayer))
                {
                    targetPos.y = hit.point.y + segmentHeightOffset;
                }

                segment.segmentTransform.position = Vector3.Lerp(
                    segment.segmentTransform.position,
                    targetPos,
                    Time.deltaTime * segmentSmoothSpeed
                );

                // Improved rotation handling
                Vector3 lookDirection = (segment.segmentTransform.position - previousSegmentPos).normalized;
                if (lookDirection != Vector3.zero)
                {
                    segment.segmentTransform.rotation = Quaternion.Lerp(
                        segment.segmentTransform.rotation,
                        Quaternion.LookRotation(lookDirection),
                        Time.deltaTime * segmentSmoothSpeed
                    );
                }

                previousSegmentPos = segment.segmentTransform.position;
            }
        }
    }

    private Vector3 GetDelayedPosition(WormSegment segment)
    {
        float targetTime = Time.time - segment.followDelay;
        Vector3 delayedPosition = segment.targetPosition;
        
        while (segment.positionHistory.Count > 0 && 
               segment.positionHistory.Peek().timestamp < targetTime)
        {
            delayedPosition = segment.positionHistory.Dequeue().position;
        }
        
        return delayedPosition;
    }

    private void EnforceMinimumSegmentDistances()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            for (int j = i + 1; j < segments.Count; j++)
            {
                if (segments[i].segmentTransform != null && segments[j].segmentTransform != null)
                {
                    Vector3 dir = segments[j].segmentTransform.position - segments[i].segmentTransform.position;
                    float distance = dir.magnitude;
                    
                    if (distance < minSegmentDistance)
                    {
                        Vector3 pushDir = dir.normalized;
                        float pushAmount = (minSegmentDistance - distance) * 0.5f;
                        
                        segments[i].segmentTransform.position -= pushDir * pushAmount;
                        segments[j].segmentTransform.position += pushDir * pushAmount;
                        
                        segments[i].targetPosition = segments[i].segmentTransform.position;
                        segments[j].targetPosition = segments[j].segmentTransform.position;
                    }
                }
            }
        }
    }

    private float GetGroundHeight(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 200f, groundLayer))
        {
            return hit.point.y;
        }
        return position.y;
    }

    private void RecordPosition(Vector3 position)
    {
        TimestampedPosition timestampedPos = new TimestampedPosition(position, Time.time);
        
        foreach (var segment in segments)
        {
            if (segment.positionHistory.Count >= positionHistoryLimit)
            {
                segment.positionHistory.Dequeue();
            }
            segment.positionHistory.Enqueue(timestampedPos);
        }
    }

    private void UpdateSegmentPosition(WormSegment segment)
    {
        float targetTime = Time.time - segment.followDelay;
        
        while (segment.positionHistory.Count > 0 && 
               segment.positionHistory.Peek().timestamp < targetTime)
        {
            segment.targetPosition = segment.positionHistory.Dequeue().position;
        }
        
        Vector3 desiredPosition = Vector3.Lerp(
            segment.segmentTransform.position,
            segment.targetPosition,
            Time.deltaTime * segmentFollowSpeed
        );

        if (!isBurrowed)
        {
            RaycastHit hit;
            if (Physics.Raycast(segment.segmentTransform.position + Vector3.up, Vector3.down, out hit, 1.1f, groundLayer))
            {
                desiredPosition.y = hit.point.y + 0.1f;
            }
        }

        segment.segmentTransform.position = desiredPosition;
    }

    private IEnumerator MoveToPosition(Vector3 startPos, Vector3 endPos, float speed)
    {
        float journeyLength = Vector3.Distance(startPos, endPos);
        float startTime = Time.time;
        
        while (Vector3.Distance(transform.position, endPos) > 0.1f)
        {
            float distanceCovered = (Time.time - startTime) * speed;
            float fractionOfJourney = distanceCovered / journeyLength;
            
            transform.position = Vector3.Lerp(startPos, endPos, fractionOfJourney);
            yield return null;
        }
    }

    public override void TakeDamage(float damage, Vector3 hitPoint)
    {
        Debug.Log("Took damage in wormenemy");

        WormEye hitEye = FindClosestEye(hitPoint);
        
        if (hitEye != null && !hitEye.isDestroyed)
        {
            DamageEye(hitEye, damage);
        }
        Debug.Log("Tried to hit eye");
    }

    private WormEye FindClosestEye(Vector3 hitPoint)
    {
        WormEye closestEye = null;
        float closestDistance = float.MaxValue;

        foreach (var segment in segments)
        {
            foreach (var eye in segment.eyes)
            {
                if (!eye.isDestroyed)
                {
                    float distance = Vector3.Distance(eye.eyeTransform.position, hitPoint);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEye = eye;
                    }
                }
            }
        }

        return closestEye;
    }

    private void DamageEye(WormEye eye, float damage)
    {
        eye.health -= damage;
        
        Debug.Log("Damaged Eye");

        if (eye.health <= 0 && !eye.isDestroyed)
        {
            DestroyEye(eye);
            CheckForDeath();
            Debug.Log("Destroyed eye");
        }
    }

    private void DestroyEye(WormEye eye)
    {
        eye.isDestroyed = true;
        
        if (eye.destroyedEffect != null)
        {
            Instantiate(eye.destroyedEffect, eye.eyeTransform.position, Quaternion.identity);
        }
        
        if (eyeDestroySound) audioSource.PlayOneShot(eyeDestroySound);
        
        SpawnHealingOrb(eye.eyeTransform.position);
    }

    private void CheckForDeath()
    {
        bool allEyesDestroyed = true;
        
        foreach (var segment in segments)
        {
            foreach (var eye in segment.eyes)
            {
                if (!eye.isDestroyed)
                {
                    allEyesDestroyed = false;
                    break;
                }
            }
        }
        
        if (allEyesDestroyed)
        {
            Die();
        }
    }
}