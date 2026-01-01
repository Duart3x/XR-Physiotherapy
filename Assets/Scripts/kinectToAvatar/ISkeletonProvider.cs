using System;

namespace K4AdotNet.Samples.Unity
{
    public interface ISkeletonProvider
    {
        event EventHandler<SkeletonEventArgs> SkeletonUpdated;
    }
}
