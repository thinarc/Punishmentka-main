using _Project.Scripts.EntryPoints;

namespace _Project.Scripts.Interactive
{
    public class DoorView : ItemView
    {
        public bool toStreet;
        
        public override void SeeView()
        {
            if (toStreet)
            {
                FindAnyObjectByType<GameEntryPoint>().DoChangeScene(2);
                return;
            }
            FindAnyObjectByType<GameEntryPoint>().DoChangeScene(1);
            FindAnyObjectByType<GameEntryPoint>().ChangeLightM(0.74f);
        }

        public override void UnseeView() { }
    }
}