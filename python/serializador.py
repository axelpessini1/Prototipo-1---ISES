# Python/serializador.py

def crear_resultado(
    ok=False,
    output="",
    variables=None,
    commands=None,
    error=None
):
    return {
        "ok": ok,
        "output": output,
        "variables": variables or {},
        "commands": commands or [],
        "error": error
    }