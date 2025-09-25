# 1、AOI Library Introduction

> 1. An AOI library implemented using a jump table + cross chain method.
> 2. Can do simple collision detection, client resources, and server AOI.
> 3. The test efficiency of insertion, movement and search is less than milliseconds.

### 1.1 A simple demo

```c#
// Create an AOI area. If the map is too large, you can define multiple areas.

var zone = new AoiZone();

// The display area of ​​AOI can be defined individually by each client, which can better adapt to different resolutions.

var area = new Vector2(3, 3);

// Add 50 players.

for (var i = 1; i <= 50; i++) zone.Enter(i, i, i);

// Refresh the information with key 3.

zone.Refresh(3, area, out var enters);

Console.WriteLine("---------------List of players that have joined the player scope--------------");

foreach (var aoiKey in enters)
{
    var findEntity = zone[aoiKey];
    Console.WriteLine($"X:{findEntity.X.Value} Y:{findEntity.Y.Value}");
}

// Update the coordinates of key 3.

var entity = zone.Refresh(3, 20, 20, new Vector2(3, 3), out enters);

Console.WriteLine("---------------List of players who have left player range--------------");

foreach (var aoiKey in entity.Leave)
{
    var findEntity = zone[aoiKey];
    Console.WriteLine($"X:{findEntity.X.Value} Y:{findEntity.Y.Value}");
}

Console.WriteLine("---------------Key 3 is the list of players that join the player range after moving--------------");

foreach (var aoiKey in enters)
{
    var findEntity = zone[aoiKey];
    Console.WriteLine($"X:{findEntity.X.Value} Y:{findEntity.Y.Value}");
}

// Leave the current AOI

zone.Exit(50);
```


# 2. Blog Post

[AOI algorithm implementation and principle（一）](https://zhuanlan.zhihu.com/p/56114206) [AOI algorithm implementation and principle（二）](https://zhuanlan.zhihu.com/p/345741408)
