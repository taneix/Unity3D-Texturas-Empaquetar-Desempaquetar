# Unity3D Texturas Empaquetar y Desempaquetar

Herramienta para Unity que permite gestionar canales de texturas de forma eficiente.

## 🛠️ Funcionalidades
  - **Empaquetar:** Combina Albedo, Rugosidad, Normal, Oclusión y Altura en dos texturas (CR y NOH).
  - **Desempacar:** Extrae canales individuales de una textura de máscara (por ejemplo, para terrenos).

## 📂 Instalación
  1. Descarga los scripts `.cs`.
  2. En tu proyecto de Unity, crea una carpeta llamada **Editor** dentro de **Assets**.
  3. Coloca los scripts dentro de `Assets/Editor/`.
  4. Aparecerá un menú superior llamado **Tools > Texturas**.

## 📝 ¿Para qué sirve esta herramienta?
  En Unity, el rendimiento es clave. En lugar de tener 6 texturas separadas consumiendo memoria de video (VRAM), esta herramienta las combina en solo 2 archivos. Esto optimiza el tamaño de tu juego y mejora la velocidad de    renderizado.

## ⚠️ Requisito Importante: Read/Write Enabled
  Para que el script pueda procesar los píxeles de tus imágenes, las texturas de entrada deben permitir el acceso de lectura y escritura. De lo contrario, verás un error en la consola.

  Cómo configurarlo:
  Selecciona la textura (o las texturas) en tu carpeta Project.
  En el panel Inspector, busca la casilla Read/Write Enabled.
  Actívala (que quede marcada).
  Haz clic en el botón Apply al final del Inspector.

## 📄 Licencia
  Este proyecto está bajo la Licencia MIT. Esto significa que puedes usarlo libremente en tus proyectos personales y comerciales. Para más detalles, consulta el archivo LICENSE incluido en este repositorio.

🤝 Contribuciones
  ¡Cualquier sugerencia o mejora es bienvenida! Si encuentras un error o quieres añadir una funcionalidad, siéntete libre de abrir un Issue o enviar un Pull Request.
