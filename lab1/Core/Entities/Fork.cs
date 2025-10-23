namespace DiningPhilosophers.Core
{
    namespace Fork
    {
        public class ForkView
        {
            public int Id { get; init; }
            public ForkUtils.ForkState State { get; init; }
            public string? Owner { get; init; }
        }
        public class Fork(int id)
        {
            public int Id { get; } = id;
            public ForkUtils.ForkState State { get; set; } = ForkUtils.ForkState.Available;
            public string? Owner { get; set; }

            public void Release()
            {
                State = ForkUtils.ForkState.Available;
                Owner = null;
            }

            public void Use(string name)
            {
                State = ForkUtils.ForkState.InUse;
                Owner = name;
            }

            public ForkView AsView() => new()
            {
                Id = Id,
                State = State,
                Owner = Owner
            };
        }
    }
}