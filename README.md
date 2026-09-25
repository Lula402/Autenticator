# Snake — Unity + Firebase (Serverless)

**Autora:** Luisa Fernanda García Gallego
**Curso:** Sistemas Interactivos Distribuidos

Aplicación Unity (Android) con arquitectura **serverless**: todo el backend lo gestiona **Firebase**
(Authentication + Realtime Database). No hay servidor propio.

## Funcionalidades

| Requisito | Implementación |
|---|---|
| Proyecto Firebase para Unity-Android | `Assets/google-services.json`, package `com.luisagarcia.sfd4` |
| Registro, login y recuperación de contraseña por correo | `ButtonRegister`, `ButtonLogin`, `ButtonResetPassword`, `ButtonLogout`, `AuthStateHandler` |
| Nombre de usuario en el registro | Se guarda en `users/{uid}/username` junto con el puntaje |
| Guardar puntajes de un juego funcional | Juego **Snake** (`SnakeGameManager`); `ScoreService` guarda el mejor puntaje con una transacción y el historial con `Push` |
| Tabla de puntajes más altos | `Leaderboard`: `OrderByChild("score").LimitToLast(10)` con `ValueChanged` (**tiempo real**) |
| Nombre completo visible | Pie de página en todas las pantallas (`AuthorFooter`, `ProjectInfo`) |

## El juego

Snake clásico en una cuadrícula de 24×13. La comida **roja** da +10 y alarga la serpiente; la **dorada**
aparece a veces, da +30 y desaparece a los 6 segundos. Cada comida acelera un poco la serpiente.
Chocar con el borde o con el propio cuerpo termina la partida.
Controles: flechas / WASD, o deslizar el dedo (swipe) en el celular.

## Estructura de la base de datos

```
users/
  {uid}/
    username: "luisa"
    score: 250          ← mejor puntaje (se usa para el leaderboard)
    creado: 1695651234000
scores/
  {uid}/
    {pushId}/ { score: 250, fecha: 1695651234000 }   ← historial de partidas
```

## Reglas de la Realtime Database

```json
{
  "rules": {
    "users": {
      ".read": "auth != null",
      ".indexOn": ["score"],
      "$uid": {
        ".write": "auth != null && auth.uid === $uid"
      }
    },
    "scores": {
      "$uid": {
        ".read": "auth != null && auth.uid === $uid",
        ".write": "auth != null && auth.uid === $uid"
      }
    }
  }
}
```

## Cómo abrir y ejecutar

1. Abrir el proyecto con **Unity 6000.5.8f1** con la plataforma **Android** activa.
2. Abrir `Assets/Scenes/SampleScene.unity`.
3. Si la escena aún no tiene la interfaz de Firebase: menú **SID2 → Construir escena Firebase** y guardar (Ctrl+S).
4. Dar **Play**.

La URL de la base de datos está en `Assets/Scripts/Core/FirebaseService.cs` (`DatabaseUrl`).

## Scripts

```
Assets/Scripts/
  Core/    FirebaseService (inicialización), FirebaseErrors, ScoreService, ProjectInfo
  Auth/    ButtonRegister, ButtonLogin, ButtonLogout, ButtonResetPassword, AuthStateHandler
  UI/      UIManager, NavigationButton, StatusMessage, ProfileLabels, Leaderboard, AuthorFooter
  Game/    SnakeGameManager, GameButton, SpriteFactory
  Editor/  FirebaseSceneBuilder (construye la interfaz en la escena)
```
