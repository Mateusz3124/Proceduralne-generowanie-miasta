using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// because PriorityQueue nie ma w Unity
public class PriorityQueue<T, P> where P : IComparable
{   
    // smaller priority value, higher priority 
    private List<(T element, P priority)> elements;
    public int length => elements.Count;

    public PriorityQueue() {
        elements = new List<(T, P)>();
    }

    public void Push(T element, P priority) {
        elements.Add((element, priority));
        elements.Sort((x,y) => x.priority.CompareTo(y.priority));
    }

    public T Pop() {
        if(length <= 0) {
            throw new Exception("Queue empty");
        }
        var element_to_ret = elements[0].element;
        elements.RemoveAt(0);

        return element_to_ret;
    }

    public bool Contains(T value) {
        return elements.Any(x => value.Equals(x.element));
    }
}
