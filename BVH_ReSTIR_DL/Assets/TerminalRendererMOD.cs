using UnityEngine;
using System;

public class TerminalRendererMOD : MonoBehaviour
{
    void Start()
    {
        // Writing to the Unity console/terminal
        Debug.Log("This is a log message in the terminal.");
        
        // Writing directly to the terminal (standard output)
        Console.WriteLine("This is a message from Console.WriteLine.");
        System.Diagnostics.Debug.WriteLine("This is a message from Console.WriteLine.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Example of writing more text to the terminal
            Debug.Log("Space key was pressed!");
            Console.WriteLine("This is a message from Console.WriteLine.");
            System.Diagnostics.Debug.WriteLine("This is a message from Console.WriteLine.");
        }
    }
}
