using _Project.Scripts.Interactive;
using _Project.Scripts.MiniGame;
using _Project.Scripts.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TeddyCutscene : ItemView
{
    public Blockblast special;
    public InterItem specialObject;
    
    private Animator _anim;

    private Transform pastParent;
    private Vector3 pastPos;
    
    public async override void SeeView()
    {
        _anim ??= GetComponent<Animator>();
        _anim.speed = 0;
        var player = FindAnyObjectByType<PlayerMovement>();
        pastParent = transform.parent;
        pastPos = transform.position;
        transform.SetParent(player.transform);

        await UniTask.Delay(340); 
            
        player.disable = true;
        player.SetTarget(new Vector2(-1.93f, -0.63f));

        await UniTask.Delay(2600);
        UnseeView();
    }
    
    public async override void UnseeView()
    {
        await UniTask.Delay(800);
        
        _anim.speed = 1;
        transform.SetParent(pastParent);
        transform.position = pastPos;
        
        var player = FindAnyObjectByType<PlayerMovement>();

        await UniTask.Delay(1000);
        
        specialObject.enabled = true;
        specialObject.GetComponent<Collider2D>().enabled = true;
        special.ReInvoke();
        
        await UniTask.Delay(340);
        player.disable = false;
        player.ResetTarget();
        
        enabled = false;
        GetComponent<Collider2D>().enabled = false;
    }
}
