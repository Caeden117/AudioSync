namespace AudioSync.Util.Tests;

/// <summary>
/// This class contains C# re-implementations of certain Aubio methods in order to test the results of AudioSync rewritten alternatives.
/// </summary>
internal static class AubioCompareMethods
{
    /*
     * A C# re-implementation of Aubio's Median
     */
    public static double Median(double[] arr)
    {
        var n = arr.Length;
        int low, high;
        int median;
        int middle, ll, hh;
        double tmp;

        low = 0; high = n - 1; median = (low + high) / 2;
        for (; ; )
        {
            if (high <= low) /* One element only */
                return arr[median];

            if (high == low + 1)
            {  /* Two elements only */
                if (arr[low] > arr[high])
                {
                    tmp = arr[low];
                    arr[low] = arr[high];
                    arr[high] = tmp;
                }
                return arr[median];
            }

            /* Find median of low, middle and high items; swap into position low */
            middle = (low + high) / 2;
            if (arr[middle] > arr[high])
            {
                tmp = arr[middle];
                arr[middle] = arr[high];
                arr[high] = tmp;
            }
            if (arr[low] > arr[high])
            {
                tmp = arr[low];
                arr[low] = arr[high];
                arr[high] = tmp;
            }
            if (arr[middle] > arr[low])
            {
                tmp = arr[middle];
                arr[middle] = arr[low];
                arr[low] = tmp;
            }

            /* Swap low item (now in position middle) into position (low+1) */
            tmp = arr[middle];
            arr[middle] = arr[low + 1];
            arr[low + 1] = tmp;

            /* Nibble from each end towards middle, swapping items when stuck */
            ll = low + 1;
            hh = high;
            for (; ; )
            {
                do ll++; while (arr[low] > arr[ll]);
                do hh--; while (arr[hh] > arr[low]);

                if (hh < ll)
                    break;

                tmp = arr[ll];
                arr[ll] = arr[hh];
                arr[hh] = tmp;
            }

            /* Swap middle item (in position low) back into correct position */
            tmp = arr[low];
            arr[low] = arr[hh];
            arr[hh] = tmp;

            /* Re-set active partition */
            if (hh <= median)
                low = ll;
            if (hh >= median)
                high = hh - 1;
        }
    }
}