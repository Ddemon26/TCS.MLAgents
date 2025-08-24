namespace TCS.MLAgents.PredVsPray {
    public class ConeVision : MonoBehaviour {
        [SerializeField] float viewDistance = 10f;
        [SerializeField] float viewAngle = 45f;
        [SerializeField] int rayCount = 8;
        [SerializeField] LayerMask targetLayers = -1;
        [SerializeField] bool debugDraw = false;
        
        public float ViewDistance => viewDistance;
        public float ViewAngle => viewAngle;
        public int RayCount => rayCount;
        
        public struct VisionRay {
            public float distance;
            public bool hitTarget;
            public string targetTag;
            public Vector3 hitPoint;
        }
        
        public VisionRay[] GetVisionRays() {
            VisionRay[] rays = new VisionRay[rayCount];
            
            float angleStep = viewAngle / (rayCount - 1);
            float startAngle = -viewAngle / 2f;
            
            for (int i = 0; i < rayCount; i++) {
                float currentAngle = startAngle + (angleStep * i);
                Vector3 rayDirection = Quaternion.AngleAxis(currentAngle, transform.up) * transform.forward;
                
                RaycastHit hit;
                if (Physics.Raycast(transform.position, rayDirection, out hit, viewDistance, targetLayers)) {
                    rays[i] = new VisionRay {
                        distance = hit.distance / viewDistance,
                        hitTarget = true,
                        targetTag = hit.collider.tag,
                        hitPoint = hit.point
                    };
                } else {
                    rays[i] = new VisionRay {
                        distance = 1f,
                        hitTarget = false,
                        targetTag = "",
                        hitPoint = transform.position + rayDirection * viewDistance
                    };
                }
                
                if (debugDraw) {
                    Color rayColor = rays[i].hitTarget ? Color.red : Color.green;
                    Debug.DrawRay(transform.position, rayDirection * (rays[i].hitTarget ? hit.distance : viewDistance), rayColor);
                }
            }
            
            return rays;
        }
        
        public float[] GetDistanceObservations() {
            VisionRay[] rays = GetVisionRays();
            float[] distances = new float[rayCount];
            
            for (int i = 0; i < rayCount; i++) {
                distances[i] = rays[i].distance;
            }
            
            return distances;
        }
        
        public bool[] GetHitObservations() {
            VisionRay[] rays = GetVisionRays();
            bool[] hits = new bool[rayCount];
            
            for (int i = 0; i < rayCount; i++) {
                hits[i] = rays[i].hitTarget;
            }
            
            return hits;
        }
        
        public bool CanSeeTarget(Transform target) {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
            
            if (angleToTarget <= viewAngle / 2f) {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                if (distanceToTarget <= viewDistance) {
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, directionToTarget, out hit, distanceToTarget, targetLayers)) {
                        return hit.transform == target;
                    }
                }
            }
            
            return false;
        }
        
        void OnDrawGizmos() {
            if (!debugDraw) return;
            
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            
            Vector3 leftBoundary = Quaternion.AngleAxis(-viewAngle / 2f, transform.up) * transform.forward;
            Vector3 rightBoundary = Quaternion.AngleAxis(viewAngle / 2f, transform.up) * transform.forward;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, leftBoundary * viewDistance);
            Gizmos.DrawRay(transform.position, rightBoundary * viewDistance);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + transform.forward * viewDistance, 0.5f);
        }
    }
}