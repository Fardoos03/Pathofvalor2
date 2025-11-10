using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// this scripts activates enemy batches  and open next barriers if enemy clears the area
/// </summary>
public class EnemyActivator : MonoBehaviour {
    public GameObject firstEnemyBatch;
    public GameObject[] LockedBarriers;

    public void ActivateFirstEnemyBatch() {
        if (firstEnemyBatch != null)
            firstEnemyBatch.SetActive(true);
    }
    public void OpenBarrier(int lockerID) {
        if (LockedBarriers == null || lockerID < 0 || lockerID >= LockedBarriers.Length) {
            return;
        }
        AudioController.instance?.PlaySound(SoundClip.gateOpen);
        var barrier = LockedBarriers[lockerID];
        if (barrier != null)
            Destroy(barrier);
    }

}
