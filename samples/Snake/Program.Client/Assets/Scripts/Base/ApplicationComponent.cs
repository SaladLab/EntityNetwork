﻿using System.IO;
using System.Linq;
using System.Net;
using Common.Logging;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class ApplicationComponent : MonoBehaviour // , IUserEventObserver
{
    private static List<Tuple<SendOrPostCallback, object>> _posts =
        new List<Tuple<SendOrPostCallback, object>>();

    public static ApplicationComponent Instance
    {
        get; private set;
    }

    public static bool TryInit()
    {
        if (Instance != null)
            return false;

        var go = new GameObject("_ApplicationComponent");
        Instance = go.AddComponent<ApplicationComponent>();
        DontDestroyOnLoad(go);
        return true;
    }

    private void Update()
    {
        ExecutePostCallback();
    }

    private static void ExecutePostCallback()
    {
        List<Tuple<SendOrPostCallback, object>> posts;
        lock (_posts)
        {
            if (_posts.Count == 0)
                return;
            posts = _posts;
            _posts = new List<Tuple<SendOrPostCallback, object>>();
        }

        foreach (var post in posts)
        {
            post.Item1(post.Item2);
        }
    }

    public static void Post(SendOrPostCallback callback, object state)
    {
        lock (_posts)
        {
            _posts.Add(Tuple.Create(callback, state));
        }
    }

    /*
    public void UserContextChange(TrackableUserContextTracker userContextTracker)
    {
        G.Logger.InfoFormat("UserContext: {0}", userContextTracker);
        userContextTracker.ApplyTo(G.UserContext);
    }
    */
}
