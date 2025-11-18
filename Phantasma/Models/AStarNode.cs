using System.Collections.Generic;

namespace Phantasma.Models;

public struct AStarNode
{
    public Tree Order;
    public LinkedListNode<AStarNode> Next;
    public int Cost;
    public int Goodness;
    public int Len;
    public int X;
    public int Y;
    public int Depth;
    public byte Scheduled; //scheduled:1;
}