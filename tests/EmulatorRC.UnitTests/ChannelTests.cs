using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace EmulatorRC.UnitTests
{
    public class ChannelTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public ChannelTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        private static ChannelReader<string> CreateProducer(string msg, int count)
        {
            var ch = Channel.CreateUnbounded<string>();
            Task.Run(async () =>
            {
                for (var i = 0; i < count; i++)
                {
                    await ch.Writer.WriteAsync($"{msg} {i + 1}");
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }

                ch.Writer.Complete();
            });

            return ch.Reader;
        }

        [Fact]
        public async Task Test1()
        {
            var uploader = new ScreenChannel();

            var observer1 = new ScreenObserver(_outputHelper, "sub1");
            var sub1 = uploader.Subscribe(observer1);

            var observer2 = new ScreenObserver(_outputHelper, "sub2");
            var sub2 = uploader.Subscribe(observer2);

            var producer = Task.Run(async () =>
            {
                for (var i = 0; i < 6; i++)
                {
                    await uploader.Enqueue($"message_{i + 1}");
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
            });

            await producer;
            sub1.Dispose();
            sub2.Dispose();
        }


        public class ScreenChannel : IObservable<string>
        {
            private const int MAX_QUEUE = 5;

            private readonly Channel<string> _channel;
            private readonly IObservable<string> _replyStream;
            private readonly CancellationTokenSource _cts;

            public ScreenChannel()
            {
                _cts = new CancellationTokenSource();

                var options = new BoundedChannelOptions(MAX_QUEUE)
                {
                    SingleWriter = true,
                    SingleReader = false,
                    FullMode = BoundedChannelFullMode.DropOldest
                };
                _channel = Channel.CreateBounded<string>(options);
                _replyStream = Run();
            }

            public async ValueTask Enqueue(string image, CancellationToken cancellationToken = default)
            {
                await _channel.Writer.WriteAsync(image, cancellationToken);
            }

            public IDisposable Subscribe(IObserver<string> observer)
            {
                return _replyStream.Subscribe(observer);
            }

            public IObservable<string> Run()
            {
                return Observable.Create<string>(observer =>
                {
                    async Task CreateConsumer()
                    {
                        try
                        {
                            await foreach (var image in _channel.Reader.ReadAllAsync(_cts.Token))
                            {
                                observer.OnNext(image);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            /*ignored*/
                        }
                        catch (Exception e)
                        {
                            observer.OnError(e);
                        }
                        finally
                        {
                            _channel.Writer.Complete();
                            observer.OnCompleted();
                        }
                    }

                    _ = CreateConsumer();

                    return Disposable.Empty;
                });

            }
        }

        public class ScreenSubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }

        public class ScreenObserver : IObserver<string>
        {
            private readonly ITestOutputHelper _outputHelper;
            private string _id;

            public ScreenObserver(ITestOutputHelper outputHelper, string id)
            {
                _outputHelper = outputHelper;
                _id = id;
            }

            public void OnCompleted()
            {
                Debug.WriteLine("Completed!");
            }

            public void OnError(Exception error)
            {
                Debug.WriteLine("Error: " + error.Message);
            }

            public void OnNext(string value)
            {
                _outputHelper.WriteLine(_id + " " + value);
            }
        }
    }
}
