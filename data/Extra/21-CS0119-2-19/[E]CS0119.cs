using System . Collections ; public class PlayerCamera { public Transform player , transform ; public float smoothTime = 0.3f ; public float distanceAbove = 3.0f ; public float distanceAway = 5.0f ; private float yVelocity = 0.0f ; void Update ( ) { float yAngle = Mathf . SmoothDampAngle ( transform . eulerAngles . y , player . eulerAngles . y , ref yVelocity , smoothTime ) ; Vector3 newPosition = player . position ; newPosition += Quaternion . Euler ( 0 , yAngle , 0 ) * Vector3 ( 0.0f , distanceAbove , - distanceAway ) ; gameObject . transform . position = newPosition ; gameObject . transform . LookAt ( player ) ; } private PlayerCamera gameObject = new PlayerCamera ( ) ; } public class Transform { public Vector3 position , rotation , eulerAngles ; public void LookAt ( Transform another ) { } } public class Vector3 { public Vector3 ( float x , float y , float z ) { this . x = x ; this . y = y ; this . z = z ; } public float x , y , z ; public static Vector3 operator + ( Vector3 l , Vector3 r ) { return new Vector3 ( l . x + r . x , l . y + r . y , l . z + r . z ) ; } public static Vector3 operator * ( Vector3 l , Vector3 r ) { return new Vector3 ( l . x * r . x , l . y * r . y , l . z * r . z ) ; } } public class Quaternion { public static Vector3 Euler ( float x , float y , float z ) { return new Vector3 ( x , y -- , z ++ ) ; } } public class Input { public static bool GetMouseButton ( int x ) { return x > 0 ; } public static float GetAxis ( string name ) { /* inserted */ int _30 = 13 ; return name . Length ; } } public class Mathf { public static float SmoothDampAngle ( float current , float target , ref float currentVelocity , float smoothTime ) { currentVelocity = current ; return target ; } } public class Program { public static void Main ( ) { } } 
