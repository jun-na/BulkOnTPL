using R3;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

SimpleAction();
//BulkAction();
//Parallel();

void SimpleAction()
{
    // 入力、加工等を行う
    var transformBlock = new TransformBlock<char, char>(c =>
    {
        return c.ToString().ToUpper()[0];
    });

    // 加工データを貰って実行
    var actionBlock = new ActionBlock<char>(c =>
    {
        Console.WriteLine("");
        Console.WriteLine($"Out! {c}");
    });

    // ===ブロックをリンク===
    transformBlock.LinkTo(actionBlock, new DataflowLinkOptions
    {
        PropagateCompletion = true
    });

    for (; ; )
    {
        var key = Console.ReadKey();
        if (key.Key == ConsoleKey.Escape)
            break;

        transformBlock.Post(key.KeyChar);
    }
}

void BulkAction()
{
    // 入力、加工等を行う
    var transformBlock = new TransformBlock<char, char>(c =>
    {
        return c.ToString().ToUpper()[0];
    });

    // 規定の個数分まとまるまで待つブロック
    var batchBlock = new BatchBlock<char>(5);

    // 配列を貰って実行
    var actionBlock = new ActionBlock<char[]>(c =>
    {
        Console.WriteLine("");
        Console.WriteLine($"Bulk! {string.Join(",", c.ToArray())}");
    });

    // ===ブロックをリンク===
    transformBlock.LinkTo(batchBlock, new DataflowLinkOptions
    {
        PropagateCompletion = true
    });

    batchBlock.LinkTo(actionBlock, new DataflowLinkOptions
    {
        PropagateCompletion = true
    });

    var debounceValue = new ReactiveProperty<char>();
    debounceValue
        .Debounce(TimeSpan.FromSeconds(3))
        .Subscribe(x =>
        {
            batchBlock.TriggerBatch();
        });

    for (; ; )
    {
        var key = Console.ReadKey();
        if (key.Key == ConsoleKey.Escape)
            break;

        transformBlock.Post(key.KeyChar);
        debounceValue.Value = key.KeyChar;
    }
}

void Parallel()
{
    var sw = new Stopwatch();
    sw.Start();

    var transformBlock = new TransformBlock<int, int>(i =>
    {
        Console.WriteLine($"{sw.ElapsedMilliseconds}ms Transform Start {i}");
        Task.Delay(1000).Wait(); 
        Console.WriteLine($"{sw.ElapsedMilliseconds}ms Transform End {i}");
        return i;
    });

    var actionBlock = new ActionBlock<int>(i =>
    {
        Console.WriteLine($"{sw.ElapsedMilliseconds}ms Action Start {i}");
        Task.Delay(2000).Wait();
        Console.WriteLine($"{sw.ElapsedMilliseconds}ms Action End {i}");
    });

    transformBlock.LinkTo(actionBlock, new DataflowLinkOptions
    {
        PropagateCompletion = true
    });

    while (true)
    {
        Task.Run(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                transformBlock.Post(i);
            }
            transformBlock.Complete();
            Console.WriteLine($"{sw.ElapsedMilliseconds}ms Complete Post");
        });
        var key = Console.ReadKey();
        if (key.Key == ConsoleKey.Escape)
            break;
   }

}