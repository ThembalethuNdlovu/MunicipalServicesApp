using System;
using MunicipalServicesApp.DataStructures;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// The priority queue from Part 2. It is now a thin subclass of the generic
    /// BinaryHeap in DataStructures, so the featured-events list and the event
    /// recommendations keep working unchanged while the heap code lives in one place.
    /// The item that compares as "smallest" is dequeued first.
    /// </summary>
    public class MinPriorityQueue<T> : BinaryHeap<T>
    {
        public MinPriorityQueue(Comparison<T> compare) : base(compare)
        {
        }
    }
}