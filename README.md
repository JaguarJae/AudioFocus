🎧 AudioFocus

AudioFocus es una aplicación en C# que gestiona automáticamente qué aplicación de audio debe reproducirse en Windows, evitando conflictos entre múltiples fuentes de sonido.

Cuando varias aplicaciones intentan reproducir audio (Spotify, navegador, etc.), AudioFocus decide cuál debe estar activa y pausa el resto.

⚙️ Características
Detección automática de sesiones multimedia activas
Priorización de aplicaciones (ej: Spotify sobre otras)
Pausado automático de sesiones en segundo plano
Cambio dinámico de foco de audio
Integración con la API de Windows (GlobalSystemMediaTransportControlsSession)
🧠 Cómo funciona

AudioFocus utiliza la API de Windows:

GlobalSystemMediaTransportControlsSessionManager
GlobalSystemMediaTransportControlsSession

El flujo principal es:

Se obtienen todas las sesiones activas de audio
Se detecta cuál está en reproducción (PlaybackStatus)
Se decide cuál debe ser la sesión activa
Se pausan las demás sesiones
Se actualiza dinámicamente cuando hay cambios (PlaybackInfoChanged)
🚀 Instalación
Opción 1: Ejecutable

Descarga la última versión desde Releases.

Opción 2: Compilar desde código
git clone https://github.com/tuusuario/audiofocus.git
cd audiofocus
dotnet build
dotnet run
▶️ Uso
Ejecuta la aplicación
Abre varias apps de audio (Spotify, YouTube, etc.)
AudioFocus gestionará automáticamente cuál debe reproducirse
🛠️ Tecnologías
C# (.NET 8)
Windows Media Control API
WinForms (para UI / tray)
