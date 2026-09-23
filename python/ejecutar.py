# Python/ejecutar.py

import sys
import json
import io
import contextlib

from MiniAPI import GameAPI
from serializador import crear_resultado


def ejecutar_codigo(codigo):
    api = GameAPI()
    
    entorno = {
        "moveRight": api.moveRight,
        "moveLeft": api.moveLeft,
        "moveUp": api.moveUp,
        "moveDown": api.moveDown,
        "collect": api.collect,
        "say": api.say
    }

    salida = io.StringIO()

    try:
        with contextlib.redirect_stdout(salida):
            exec(codigo, entorno)

        variables = {}

        for nombre, valor in entorno.items():
            if not nombre.startswith("__"):
                if isinstance(valor, (int, float, str, bool, type(None))):
                    variables[nombre] = valor

        return crear_resultado(
            ok=True,
            output=salida.getvalue(),
            variables=variables,
            commands=api.get_commands()
        )

    except Exception as e:
        return crear_resultado(
            ok=False,
            output=salida.getvalue(),
            error=str(e)
        )


def main():
    codigo = sys.stdin.read()

    resultado = ejecutar_codigo(codigo)

    print(json.dumps(resultado, ensure_ascii=False))


if __name__ == "__main__":
    main()