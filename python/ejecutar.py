import sys

# Leer todo el código que Godot envía
codigo = sys.stdin.read()

try:
    # Entorno donde se ejecutará el código
    entorno = {}

    # Ejecutar el código recibido
    exec(codigo, entorno)

    print("EJECUCION_EXITOSA")
    print("Test Desde El Python")


except Exception as e:
    print("ERROR")
    print(str(e))