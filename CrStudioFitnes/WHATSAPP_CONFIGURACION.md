# WhatsApp Cloud API - CVStudioFitness

## Endpoint público

Configurar en Meta como Callback URL:

https://cvstudiofitness.com/api/whatsapp/webhook

El endpoint GET se usa para la verificación de Meta y el POST recibe:
- estados del mensaje: sent, delivered, read, failed;
- clic en Confirmar reserva;
- clic en Cancelar reserva.

## Plantillas necesarias

Crear y aprobar en WhatsApp Manager dos plantillas de categoría Utility.

### 1. recordatorio_reserva_confirmacion

Idioma configurado por defecto: es

Cuerpo con exactamente 3 variables:

Hola {{1}}.
Te recordamos tu reserva en CVStudioFitness para el {{2}} a las {{3}}.

¿Vas a asistir?

Botones Quick Reply, en este orden:
1. Confirmar reserva
2. Cancelar reserva

El sistema envía payloads internos:
- CONFIRMAR_<IdReserva>
- CANCELAR_<IdReserva>

### 2. recordatorio_reserva_proxima

Cuerpo con exactamente 3 variables:

Hola {{1}}.
Tu reserva en CVStudioFitness es hoy {{2}} a las {{3}}.
¡Te esperamos!

Esta plantilla no necesita botones porque se envía aproximadamente una hora antes.

## Variables de configuración

No guardar tokens reales en GitHub.

Configurar en el servidor, tanto para el Windows Service EnviaCorreoNotificaciones como para el sitio IIS cuando corresponda:

WhatsApp__Enabled=true
WhatsApp__GraphApiVersion=v27.0
WhatsApp__PhoneNumberId=<PHONE_NUMBER_ID>
WhatsApp__AccessToken=<TOKEN_PERMANENTE>
WhatsApp__TemplateNextDay=recordatorio_reserva_confirmacion
WhatsApp__TemplateNextHour=recordatorio_reserva_proxima
WhatsApp__LanguageCode=es
WhatsApp__VerifyToken=<TOKEN_ELEGIDO_POR_USTED>
WhatsApp__AppSecret=<APP_SECRET_DE_META>

El worker necesita principalmente Enabled, GraphApiVersion, PhoneNumberId, AccessToken y las plantillas.
El sitio web necesita VerifyToken y AppSecret para validar el webhook, y PhoneNumberId/AccessToken para responder al usuario después de Confirmar/Cancelar.

## Suscripción del webhook

En la aplicación de Meta:
1. Añadir el producto WhatsApp.
2. Configurar Webhooks.
3. Callback URL: https://cvstudiofitness.com/api/whatsapp/webhook
4. Verify token: el mismo valor configurado como WhatsApp__VerifyToken.
5. Suscribir el campo messages de la cuenta de WhatsApp Business.

## Base de datos

La migración 20261106203000_WhatsAppReservas crea ReservaWhatsAppReminder.

Ejecutar en el proyecto CrStudioFitnes:

Update-Database

La tabla conserva por reserva y tipo de recordatorio:
- teléfono;
- wamid;
- estado de envío;
- intentos y errores;
- estado de entrega/lectura;
- respuesta Confirmada/Cancelada/Rechazada;
- fecha de respuesta.

## Reglas implementadas

- Solo se envían recordatorios para Reservas.Activa = true.
- Una reserva cancelada no recibe correo ni WhatsApp.
- Día anterior: correo + WhatsApp con Confirmar/Cancelar.
- Una hora antes: correo + WhatsApp informativo.
- Confirmar registra el evento en movimientosreservas sin alterar las lecciones.
- Cancelar por WhatsApp respeta la misma regla de mínimo 1 hora de anticipación.
- Al cancelar correctamente: Reserva.Activa=false, se devuelve 1 lección y se registra en movimientosreservas.
- Si ya está cancelada o llegó demasiado tarde, no se devuelve ninguna lección.
- Correo y WhatsApp tienen tracking independiente.
