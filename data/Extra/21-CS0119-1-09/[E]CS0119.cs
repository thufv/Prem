using System . Collections ; public class CameraTest { public Transform target , transform ; public float distance = 10.0f ; public float xSpeed = 250.0f ; public float ySpeed = 120.0f ; public float yMinLimit = - 20f ; public float yMaxLimit = 80f ; private float x = 0.0f ; private float y = 0.0f ; void Start ( ) { x = transform . eulerAngles . y ; y = transform . eulerAngles . x ; } public void LateUpdate ( ) { if ( target != null ) { if ( Input . GetMouseButton ( 1 ) ) { x += Input . GetAxis ( "Mouse X" ) * xSpeed * 0.02f ; y -= Input . GetAxis ( "Mouse Y" ) * ySpeed * 0.02f ; } y = ClampAngle ( y , yMinLimit , yMaxLimit ) ; transform . rotation = Quaternion . Euler ( y , x , 0 ) ; transform . position = transform . rotation * Vector3 ( 0.0f , 0.0f , - distance ) + target . position ; } } static float ClampAngle ( float angle , float min , float max ) { if ( angle < - 360 ) angle += 362 /* updated */ ; if ( angle > 360 ) angle -= 360 ; return Clamp ( angle , min , max ) ; } static float Clamp ( float angle , float min , float max ) { return min ; } static void Main ( ) { } } public class Transform { public Vector3 position , rotation , eulerAngles ; } public class Vector3 { public Vector3 ( float x , float y , float z ) { this . x = x ; this . y = y ; this . z = z ; } public float x , y , z ; public static Vector3 operator + ( Vector3 l , Vector3 r ) { return new Vector3 ( l . x + r . x , l . y + r . y , l . z + r . z ) ; } public static Vector3 operator * ( Vector3 l , Vector3 r ) { return new Vector3 ( l . x * r . x , l . y * r . y , l . z * r . z ) ; } } public class Quaternion { public static Vector3 Euler ( float x , float y , float z ) { return new Vector3 ( x , y -- , z ++ ) ; } } public class Input { public static bool GetMouseButton ( int x ) { return x > 0 ; } public static float GetAxis ( string name ) { return name . Length ; } } 
