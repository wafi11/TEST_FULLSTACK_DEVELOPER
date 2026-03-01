// 1
// 1 2
// 1 2 3
// 1 2 3 4
// 1 2 3 4 5


int N = 6;
for (int i = 1; i < N; i++)
{   
    int start = i <= 1 ? 1 : i-1;
    for (int j = 0; j < i; j++)
    {   
        Console.Write(start + j);
        if (j < i -1) Console.Write("\t");
    }

    Console.WriteLine();
}