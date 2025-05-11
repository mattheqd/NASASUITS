//* Dispatches/sends messages to the main thread from the websocket thread
//* used for transmitting audio transcripts to the main thread
//* on unity this will be stored in a game object in the scene
using UnityEngine;
using System;
using System.Collections.Generic;

public class UnityMainThreadDispatcher : MonoBehaviour {
    // _ means that this is a private static variable
    // readonly and static protects the variable from being modified from the backend
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<Action> _executionQueue = new Queue<Action>();
    
    // create a public static instance of the dispatcher which can be accessed by other scripts
    // the public instance allows other scripts to call the dispatcher without direct access
    public static UnityMainThreadDispatcher _instance { get; private set; }

    // when initialized, check if the instance is already set
    // destroy existing instances to reset the thread
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    // process all incoming actions on the main thread
    private void Update() {
        lock (_executionQueue) { // prevents other threads from accessing the queue by "locking" it
            while (_executionQueue.Count > 0) {
                _executionQueue.Dequeue().Invoke(); // dequeue and execute each action
            }
        }
    }
    // allow other scripts to add actions to the queue
    public void Enqueue(Action action) {
        lock (_executionQueue) {
            _executionQueue.Enqueue(action);
        }
    }
}