import { FetchError } from "../api.js"
import { Component } from "../component/index.js"
import { ERROR_IMAGE, WARN_IMAGE } from "../resources/index.js"
import { ListComponent } from "./list.js"

const ERROR_REMOVAL_TIME_MS = 10000

const errorListElement = document.getElementById("error-list")
const errorListComponent = new ListComponent<ErrorComponent>([], { listClasses: ["error-list"], elementLiClasses: ["error-element"] })
if (errorListElement) {
    errorListComponent.mount(errorListElement)
}

let alertedErrorListNotFound = false

export function showErrorPopup(message: string, fatal: boolean = false, errorObject?: any) {
    // EN: Guard against invalid or empty messages / FR: Protection contre les messages invalides ou vides
    if (!message || message === "undefined" || message === "null" || message === "[object Object]") {
        console.warn("[Moonlight] Suppressed empty or undefined error popup:", errorObject);
        return;
    }

    // EN: Filter out pointer-lock errors which are common and harmless on mobile
    // FR: Filtrer les erreurs de pointer-lock qui sont courantes et inoffensives sur mobile
    const lowerMsg = message.toLowerCase();
    if (lowerMsg.includes("pointer-lock") || lowerMsg.includes("pointerlock") || lowerMsg.includes("pointer lock")) {
        console.log("[Moonlight] Suppressed pointer-lock error:", message);
        return;
    }

    console.error(message, errorObject)

    if (!errorListElement) {
        if (!alertedErrorListNotFound) {
            console.warn("couldn't find the error element");
            alertedErrorListNotFound = true
        }
        // alert(message) // EN: Avoid alerts in production / FR: Eviter les alertes en production
        return;
    }

    let error
    if (fatal) {
        error = new ErrorComponent(message, ERROR_IMAGE)
    } else {
        error = new ErrorComponent(message, WARN_IMAGE)
    }

    errorListComponent.append(error)

    setTimeout(() => {
        errorListComponent.removeValue(error)
    }, ERROR_REMOVAL_TIME_MS)
}

function handleError(event: ErrorEvent) {
    // EN: Fallback to event.message if event.error is missing
    // FR: Utiliser event.message si event.error est manquant
    const message = event.error ? `${event.error}` : (event.message || "Unknown error");
    const fatal = event.error instanceof FetchError;

    // EN: Only show popups for FetchErrors or explicit fatal errors to avoid spamming non-critical JS errors
    // FR: Ne montrer les popups que pour les FetchErrors ou les erreurs fatales pour éviter le spam d'erreurs JS non critiques
    if (fatal) {
        showErrorPopup(message, fatal, event);
    } else {
        console.warn("[Moonlight] Suppressed non-fatal background error:", message);
    }
}

function handleRejection(event: PromiseRejectionEvent) {
    const reason = event.reason;
    const fatal = reason instanceof FetchError;
    
    // EN: Ignore AbortErrors (timeouts/manual cancels) and generic rejections without reason
    // FR: Ignorer les AbortErrors (timeouts) et les rejets génériques sans raison
    if (!reason || reason.name === "AbortError" || reason === "undefined") {
        return;
    }

    const message = `${reason}`;

    // EN: Only show popups for FetchErrors or critical rejections
    // FR: Ne montrer les popups que pour les FetchErrors ou les rejets critiques
    if (fatal || (message && message !== "null" && message !== "Unhandled promise rejection" && !message.includes("pointer-lock"))) {
        // EN: Still, let's avoid toast flood for rejections unless they are FetchErrors
        if (fatal) {
            showErrorPopup(message, fatal, event);
        } else {
            console.warn("[Moonlight] Background promise rejection:", message);
        }
    }
}

window.addEventListener("error", handleError)
window.addEventListener("unhandledrejection", handleRejection)

class ErrorComponent implements Component {
    private messageElement: HTMLElement = document.createElement("p")
    private imageElement: HTMLImageElement = document.createElement("img")

    constructor(message: string, image: string) {
        // EN: Ensure the message is displayed clearly / FR: S'assurer que le message est affiché clairement
        this.messageElement.innerText = message
        this.messageElement.classList.add("error-message")

        this.imageElement.src = image
        this.imageElement.classList.add("error-image")
    }

    mount(parent: Element): void {
        parent.appendChild(this.imageElement)
        parent.appendChild(this.messageElement)
    }
    unmount(parent: Element): void {
        parent.removeChild(this.imageElement)
        parent.removeChild(this.messageElement)
    }
}