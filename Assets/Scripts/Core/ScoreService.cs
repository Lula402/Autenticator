using System;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

// Guarda los puntajes del jugador en la Realtime Database.
public static class ScoreService
{
    public static void SubmitScore(int score, Action<bool, bool, long> onComplete)
    {
        FirebaseUser user = FirebaseService.Auth.CurrentUser;
        if (user == null)
        {
            onComplete?.Invoke(false, false, 0);
            return;
        }

        SaveToHistory(user.UserId, score);

        bool isNewRecord = false;
        FirebaseService.Users.Child(user.UserId).Child("score").RunTransaction(mutableData =>
        {
            long currentBest = FirebaseService.ToLong(mutableData.Value);
            isNewRecord = score > currentBest || mutableData.Value == null;
            if (isNewRecord)
            {
                mutableData.Value = score;
            }
            return TransactionResult.Success(mutableData);
        }).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error al guardar el puntaje: " + task.Exception);
                onComplete?.Invoke(false, false, 0);
                return;
            }

            long best = FirebaseService.ToLong(task.Result.Value);
            onComplete?.Invoke(true, isNewRecord && best == score, best);
        });
    }

    private static void SaveToHistory(string userId, int score)
    {
        var entry = new Dictionary<string, object>
        {
            { "score", score },
            { "fecha", ServerValue.Timestamp }
        };

        FirebaseService.Scores.Child(userId).Push().SetValueAsync(entry).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogWarning("No se pudo guardar el historial de la partida: " + task.Exception);
            }
        });
    }
}
