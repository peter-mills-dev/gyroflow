using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace HeliumDreamsTools
{
    /// <summary>
    /// Gyro / Touch Swipe camera Script v1.0
    /// By Peter Mills 
    /// Questions or Comments at
    /// peter.mills@heliumdreams.com.au
    /// </summary>
    public class GyroscopicTouchCamera : MonoBehaviour
    {
        /// <summary>
        /// Static reference emulating singlton pattern
        /// Reference script without reference using GyroscopicTouchCamera.instance
        /// Only use one GyroscopicTouchCamera at a time
        /// </summary>
        public static GyroscopicTouchCamera instance;

        /// <summary>
        /// Is the Gyroscope Currently active? 
        /// Get current staus and Set to update
        /// </summary>
        public bool isGyroActive;

        /// <summary>
        /// Speed to swap between gyro and touch rotations
        /// </summary> 
        public float transitionSpeed = 0.1f;

        /// <summary>
        /// When this box is checked the Y rotation can be changed by swiping left and right while the gyro is active
        /// </summary>
        public bool touchAlwaysRotatesGyro;

        /// <summary>
        /// 
        /// </summary>
        public bool useCustomTouchEventManager;

        /// <summary>
        ///Tracks the difference in Y rotation between the current camera rotation and the Gyroscopic direction
        /// </summary>
        private float gyroYOffsetAmount;
        /// <summary>
        ///Tracks the difference in X rotation between the current camera rotation and the Gyroscopic direction
        /// </summary>
        private float gyroXOffsetAmount;

        /// <summary>
        ///The Gyroscopes current level of influence on the camera rotation.
        ///0 is none and 1 is all.
        ///Is blended betweeb 0 and 1 when isGyroActive changes based on transitionSpeed
        /// </summary>
        private float gyroRotationInfluence;

        /// <summary>
        /// Keep the original Y rotation of the camera as an offset when recalibrating
        /// </summary>
        public bool setCustomYOffsetToYRotationAtStartup;

        /// <summary>
        /// The offset to apply to the Y rotation in addition to the offset from the camera rotation found when recalibrating
        /// </summary>
        [Tooltip("The offset to apply to the Y rotation in addition to the offset from the camera rotation found when recalibrating")]
        public float customYOffset;

        #region Touch Variables
        //Holds the current touch rotation before specific offsets based on the gyro position are made
        Quaternion touchRotation = Quaternion.identity;

        private Vector2 lastTouch;

        private float newXRotation, newYRotation;

        Vector2 touchRotationDelta;
        int fingerID;

        /// <summary>
        /// The Minimum rotation allowed when gyroscope is disabled. Used to prevent flipping by crossing poles.
        /// </summary>
        public float minXRotation = -80;
        /// <summary>
        /// The Minimum rotation allowed when gyroscope is disabled. Used to prevent flipping by crossing poles.
        /// </summary>
        public float maxXRotation = 80;

        /// <summary>
        /// Speed multiplier applied when swiping to rotate. 
        /// </summary>
        public float rotationSpeed = 5;
        #endregion

        #region Smoothed Gyro
        /// <summary>
        /// Holds the current Gyro Rotation each frame
        /// </summary>
        Quaternion attitude;

        /// <summary>
        /// Keeps track of Smoothing Samples number of previous rotations
        /// </summary>
        Queue<Quaternion> averageList;

        /// <summary>
        /// 
        /// </summary>
        Quaternion smoothedGyroRotation;

        /// <summary>
        /// used to toggle whether the smoothing settings are applied to the Gyro Rotation
        /// </summary>
        public bool isSmooth;

        /// <summary>
        /// The number of smoothing samples to keep track of and average
        /// A larger number should equal a Smoother Camera with larger delay
        /// </summary>
        public int smoothingSamples = 40;

        /// <summary>
        /// The speed at which the camera moves from its current position to the current smoothed average
        /// </summary>
        public float chaseSpeed = 4.0f;
        #endregion

        // Use this for initialization
        void Start()
        {
            if (instance == null)
            {
                instance = this;
            }
            if (instance != this)
            {
                Debug.LogError("There is more than once GryoScopicTouchCamera. There shouldn't be");
                Destroy(this);
                return;
            }

            if(setCustomYOffsetToYRotationAtStartup)
                customYOffset = transform.rotation.eulerAngles.y;

            //Initalize the Gyro at the hardware level to calculate offsets even if starting in touch mode
            Input.gyro.enabled = true;

            if (isGyroActive)
            {
                gyroRotationInfluence = 1;
            }
            else
            {
                gyroRotationInfluence = 0;
            }

            averageList = new Queue<Quaternion>();

        }

        // Update is called once per frame
        void Update()
        {
            //Update the touch rotation
            UpdateTouchRotation();

            //Move the Gyro Influence in the right direction if needed
            if (isGyroActive)
            {
                if (gyroRotationInfluence < 1)
                {
                    gyroRotationInfluence += Time.deltaTime / transitionSpeed;
                }
            }
            else
            {
                if (gyroRotationInfluence > 0)
                {
                    gyroRotationInfluence -= Time.deltaTime / transitionSpeed;
                }
            }

            //Find the correct Gyro Rotation value based on isSmooth
            Quaternion gyroRotation = Quaternion.identity;
            if (isSmooth)
            {
                gyroRotation = UpdateSmoothedGyroscopicRotation();
            }
            else
            {
                gyroRotation = GetGyroscopicRotationWithOffset();
            }

            //Apply the rotation
            transform.localRotation = Quaternion.Slerp(touchRotation, gyroRotation, gyroRotationInfluence);
        }

        #region Gyroscopic Rotation 

        /// <summary>
        /// returns a rotation moving the actual rotation toward an average of the last smoothingSamples offset gyro rotations at a speed of chaseSpeed
        /// </summary>
        /// <returns></returns>
        Quaternion UpdateSmoothedGyroscopicRotation()
        {
            attitude = GetGyroscopicRotationWithOffset();

            //Add the current rotation to the queue and remove any excess
            averageList.Enqueue(attitude);
            while (averageList.Count > smoothingSamples)
                averageList.Dequeue();

            //Extract the up and foward vectors from the averageList Quaternions and total them
            Vector3 upVectorAverage = Vector3.zero;
            Vector3 fowardVectorAverage = Vector3.zero;
            foreach (Quaternion singleRotation in averageList)
            {
                upVectorAverage += singleRotation * Vector3.up;
                fowardVectorAverage += singleRotation * Vector3.forward;
            }

            //Use the normalized total Up and Foward vectors to get the correct average rotation
            upVectorAverage.Normalize();
            fowardVectorAverage.Normalize();

            Quaternion averageRotation = Quaternion.LookRotation(fowardVectorAverage, upVectorAverage);

            Debug.DrawRay(transform.position, averageRotation * Vector3.forward * 2, Color.black);

            //Move toward the correct average rotation at chase speed
            smoothedGyroRotation = Quaternion.Slerp(transform.rotation, averageRotation, Time.deltaTime * chaseSpeed);
            Debug.DrawRay(transform.position, smoothedGyroRotation * Vector3.forward * 2, Color.cyan);
            return smoothedGyroRotation;
        }

        /// <summary>
        /// Returns the transformed raw gyroscopic rotation offset along the Y axis by the amount defined by inital orienation and touch movement
        /// </summary>
        /// <returns></returns>
        Quaternion GetGyroscopicRotationWithOffset()
        {
            if (touchAlwaysRotatesGyro)
            {
                ///TODO: <see cref="touchRotationDelta"/> is in screen space, but should probably be transformed based on the Z rotation of the camera so that when the phone is tilted it gives consistent results
                gyroXOffsetAmount += touchRotationDelta.y;
                gyroYOffsetAmount += touchRotationDelta.x;
                touchRotationDelta = Vector2.zero;
            }
            Quaternion offsetYRotation = Quaternion.Euler(0, gyroYOffsetAmount + customYOffset , 0);
            Quaternion offsetXRotation = Quaternion.Euler(gyroXOffsetAmount, 0, 0);
            return offsetYRotation * offsetXRotation * GetGyroscopicRotationRaw();
        }

        /// <summary>
        /// Returns the raw gyro data transformed to align as if it was the camera on the back of the phone
        /// </summary>
        /// <returns></returns>
        Quaternion GetGyroscopicRotationRaw()
        {
            //Set gyroRotation to unity transform space
            Quaternion correctionRotation = Quaternion.Euler(90, 0, 0);
            return correctionRotation * new Quaternion(Input.gyro.attitude.x, Input.gyro.attitude.y, -Input.gyro.attitude.z, -Input.gyro.attitude.w);
        }
        #endregion

        #region Touch Rotation
        /// <summary>
        /// Touch rotation is currently set to use the most recent touch as authoritive 
        /// This can easily be change to first touch or any other implemeation below
        /// </summary>

        /// Called by TouchEventManager When a new touch event occurs on a trigger surface
        public void SetupNewTouch(int touchValue)
        {
            if (!useCustomTouchEventManager)
            {
                Debug.LogWarning("Custom Touch event manager is working but useCustomTouchEventManager is set to false. Should it be true?");
            }
            else
            { 
                lastTouch = Input.touches[touchValue].position;
                fingerID = Input.touches[touchValue].fingerId;

                newXRotation = touchRotation.eulerAngles.x;
                newYRotation = touchRotation.eulerAngles.y;
            }
        }

        //Called by TouchEventManager When a new touch event occurs on a trigger surface
        public void EndOldTouch(int touchValue)
        {
            if (!useCustomTouchEventManager)
            {
                Debug.LogWarning("Custom Touch event manager is working but useCustomTouchEventManager is set to false. Should it be true?");
            }
            else
            { 
                fingerID = -100;
            }
        }

        /// <summary>
        /// Updates the touchRotation value based on screen swipes and updated values depended on touchRotation
        /// </summary>
        /// <returns>Current touch rotation value</returns>
        void UpdateTouchRotation()
        {
            foreach (Touch touch in Input.touches)
            {
                Debug.Log("Finger id " + touch.fingerId);
                if (touch.phase == TouchPhase.Began)
                {
                    if (!useCustomTouchEventManager)
                    {
                        lastTouch = touch.position;
                        fingerID = touch.fingerId;

                        newXRotation = touchRotation.eulerAngles.x;
                        newYRotation = touchRotation.eulerAngles.y;
                    }
                }
                else if (touch.phase == TouchPhase.Moved)
                {
                    if (fingerID == touch.fingerId)
                    {
                        //swiping action
                        float deltaX = (lastTouch.x - touch.position.x) * Time.deltaTime * rotationSpeed;
                        float deltaY = (lastTouch.y - touch.position.y) * Time.deltaTime * rotationSpeed;

                        touchRotationDelta += new Vector2(deltaX , deltaY);

                        newXRotation -= deltaY;
                        newYRotation += deltaX;

                        lastTouch = touch.position;

                        newXRotation = SetAngle180ToNegitive180(newXRotation);
                        newXRotation = Mathf.Clamp(newXRotation, minXRotation, maxXRotation);

                        touchRotation = Quaternion.Euler(newXRotation, newYRotation, 0);
                    }
                }
                else if (touch.phase == TouchPhase.Ended)
                {
                    fingerID = -100;
                }
            }
        }

        float SetAngle180ToNegitive180(float angle)
        {
            while (angle > 180 || angle < -180)
            {
                if (angle > 180)
                {
                    angle -= 360;
                }
                else if (angle < -180)
                {
                    angle += 360;
                }
            }

            return angle;
        }

        #endregion

        #region Public Methods to Update Settings
        /// <summary>
        /// Resets The Zero point for both the Gyroscope and Touch Rotations to the current camera rotation
        /// </summary>
        public void ResetZeroToCurrentCameraRotation()
        {
            gyroYOffsetAmount = -GetGyroscopicRotationRaw().eulerAngles.y;
            gyroXOffsetAmount = 0;

            touchRotation = Quaternion.identity;

            Debug.Log("Reset Zero To Current Camera Rotation");
        }

        /// <summary>
        /// Use to toggle the camera between Gyro and Touch modes
        /// </summary>
        public void ToggleGyro()
        {
            if (isGyroActive)
            {
                DisableGyro();
            }
            else
            {
                EnableGyro();
            }
        }

        /// <summary>
        /// Use to Set the camera to Gyro or Touch modes
        /// </summary>
        public void ToggleGyro(bool isActive)
        {
            if (isActive)
            {
                EnableGyro();
            }
            else
            {
                DisableGyro();
            }
        }

        /// <summary>
        /// Disable the Gyro and start using Touch mode
        /// </summary>
        public void DisableGyro()
        {
            if (isGyroActive)
            {
                Quaternion currentGyroRotation = GetGyroscopicRotationWithOffset();
                touchRotation.eulerAngles = new Vector3(currentGyroRotation.eulerAngles.x, currentGyroRotation.eulerAngles.y, 0);
            }
            isGyroActive = false;

        }

        /// <summary>
        /// Enable the Gyro and stop using Touch mode
        /// </summary>
        public void EnableGyro()
        {
            if (!isGyroActive)
            {
                gyroYOffsetAmount = -GetGyroscopicRotationRaw().eulerAngles.y + touchRotation.eulerAngles.y;
                gyroXOffsetAmount = 0;
            }
            isGyroActive = true;

        }

        /// <summary>
        /// Use to Toggle smoothing on and off
        /// </summary>
        public void ToggleSmoothing()
        {
            isSmooth = !isSmooth;
        }

        /// <summary>
        /// Set is smooth to new value
        /// </summary>
        public void ToggleSmoothing(bool newIsSmooth)
        {

            isSmooth = newIsSmooth;
        }

        /// <summary>
        /// Set the smoothing Samples to a new value
        /// </summary>
        /// <param name="numberOfSamples">New Smoothing Sample Value</param>
        public void SetSmoothingSamples(float numberOfSamples)
        {
            smoothingSamples = (int)numberOfSamples;
        }

        /// <summary>
        /// Set the chase Speed to a new value
        /// </summary>
        /// <param name="newChaseSpeed">New Chase Speed</param>
        public void SetChaseSpeed(float newChaseSpeed)
        {
            chaseSpeed = newChaseSpeed;
        }

        /// <summary>
        /// Set the <see cref="customYOffset"/> to a new value
        /// </summary>
        public void SetYOffsetRotation(float newYOffset)
        {
            customYOffset = newYOffset;
        }

        /// <summary>
        /// Set touch Always Rotates Gyros to true or false.
        /// </summary>
        public void SetUseChaseWithGyro(bool useTouch)
        {
            touchAlwaysRotatesGyro = useTouch;
        }
        #endregion
    }
}