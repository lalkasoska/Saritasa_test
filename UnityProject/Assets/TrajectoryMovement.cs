using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class TrajectoryMovement : MonoBehaviour
{
    // Trajectory is loaded as an array of Vector3-s
    public Vector3[] arr;
    public int isRandomTrajectory = 0;
    public int loop;
    public float curvature = 1f;
    public float moveTime;
    public int isClosed = 0;
    public float _timePassed = 0f;
    private float _dotToDotTime = 1f;
    private float _startTime = 0f;
    private float _movementTime = 0f;
    private bool _isMoving;
    public Vector3 _startPosition, _endPosition;
    private float _trajectoryLength = 0f;
    private float _lerpPercentage = 0f;
    private int i = 1;// Index of the dot the object is currently moving to (except 0, because it is set in StartMovement().)




    void StartMovement()
    {
        _isMoving = true;
        _startTime = Time.time;
        _startPosition = transform.position;
        _endPosition = arr[0];
    }
    void StopMovement()
    {
        _isMoving = false;
    }

    IEnumerator Start()
    {
        //That's the url the file is downloaded from.
        string url = "https://www.dropbox.com/s/xe9bocqk7epjqvq/config5.txt?dl=1";
        string savePath = "";
        //We are saving the file to the current directory.
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                savePath = string.Format("{0}/config.txt", Application.persistentDataPath);
                System.IO.File.WriteAllText(savePath, www.downloadHandler.text);
            }
        }

        try
        {
            // Creating an instance of StreamReader to read from a file.
            // The using statement also closes the StreamReader.
            using (StreamReader sr = new StreamReader(savePath))
            {
                // The config values must be in strict order: isRandom, the Dots, loop, time, isClosed.
                isRandomTrajectory = (int)Char.GetNumericValue(sr.ReadLine().Split('=')[1][1]);
                // If trajectory is random, the algorythm generates from 2 to 20 dots in Oxz plane with absolute coordinates' values no more than 30.
                if (isRandomTrajectory == 1)
                {
                    int n = UnityEngine.Random.Range(2, 20);
                    arr = new Vector3[n];
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 vector = new Vector3(UnityEngine.Random.Range(-30f, 30f), 0f, UnityEngine.Random.Range(-30f, 30f));
                        //Checking that there are no same dots in the trajectory. I know it doesn't guarantee that the trajectory doesn't cross itself, but I tried.
                        if (Array.Exists(arr, element => element == vector))
                            i--;
                        else
                            arr[i] = vector;

                    }
                }
                //Dots must be separated from each other with '|'.
                string[] dots = sr.ReadLine().Split('|');
                if (isRandomTrajectory != 1)
                {
                    arr = new Vector3[dots.Length];
                    for (int i = 0; i < dots.Length; i++)
                    {
                        //x, y and z values must be separated from each other with ' '.
                        string[] dot = dots[i].Split(' ');
                        arr[i] = new Vector3(float.Parse(dot[0]), float.Parse(dot[1]), float.Parse(dot[2]));
                    }
                }
                loop = (int)Char.GetNumericValue(sr.ReadLine().Split('=')[1][1]);
                moveTime = float.Parse(sr.ReadLine().Split('=')[1]);
                isClosed = (int)Char.GetNumericValue(sr.ReadLine().Split('=')[1][1]);
            }
        }
        catch (Exception e)
        {
            // Let the user know what went wrong.
            Console.WriteLine("The file could not be read:");
            Console.WriteLine(e.Message);
        }

        // Calculating the whole trajectory length.
        _trajectoryLength += Vector3.Distance(Vector3.zero, arr[0]);
        for (int i = 0; i < arr.Length - 1; i++)
        {
            _trajectoryLength += Vector3.Distance(arr[i], arr[i + 1]);
        }
        // Adding the end to start distance if the movement is looped.
        if (loop == 1)
            _trajectoryLength += Vector3.Distance(arr[arr.Length - 1], Vector3.zero);
    }

    // Tried to make a bezier curve, but it doesn't work as intended.
    Vector3 Bezier(Vector3 start, Vector3 end, float t, float curv)
    {
        Vector3 p0 = start;
        Vector3 p3 = end;
        Vector3 p1 = p0 - new Vector3(0, 1, 0)*curv;
        Vector3 p2 = p3 - new Vector3(0, -1, 0)*curv;
        Vector3 vector = (1 - t) * (1 - t) * (1 - t) * p0 + 3 * t * (1 - t) * (1 - t) * p1 + 3 * t * t * (1 - t) * p2 + t * t * t * p3;
        return vector;
    }
    
    void Update()
    {
        // The time it takes to go to the next dot is calculated as percentage of the whole way multiplied by the whole time so the object moves uniformly.
        _dotToDotTime = Vector3.Distance(_endPosition, _startPosition)/_trajectoryLength*moveTime;
        if (_isMoving)
        {
            

            if (_movementTime > _dotToDotTime)
            {
                _timePassed += _movementTime;
                // Then the time exceeds the limit we switch to the next dot.
                _movementTime -= _dotToDotTime;
                if (i < arr.Length) // Checking to avoid going out of range.
                    _endPosition = arr[i];
                i++;
                _startPosition = transform.position;
                if (_timePassed > moveTime) // When the movement time comes to an end.
                {
                    if (loop == 1)
                    {
                        i = 0; // Counters goes back to the very beginning.
                        //_startPosition = transform.position;
                        _endPosition = Vector3.zero; // The object now must move to the point where it started.
                        _timePassed = 0;
                    }
                    else
                    {
                        StopMovement();
                    }

                }
            }

            _movementTime += Time.deltaTime;
            _lerpPercentage = _movementTime / _dotToDotTime; // The percentage of time passed of the whole time.
            
            if (_isMoving)
            {
                transform.position = Vector3.Lerp(_startPosition, _endPosition, _lerpPercentage); // The movement itself.
                //transform.position = Bezier(_startPosition, _endPosition, _lerpPercentage, curvature);
            }
        }

        if (Input.anyKeyDown)
        {
            if ((_isMoving == false) && (arr.Length != 0))
                StartMovement();
        }
    }
}
// Everything worked perfectly fine but when I made some change that I am not able to identify now, the sphere started to lag at the end point on the first iteration of looped movement.