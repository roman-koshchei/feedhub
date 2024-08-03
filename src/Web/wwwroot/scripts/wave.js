const domParser = new DOMParser()
function replaceDocumentHtml(html) {
    const newDocument = domParser.parseFromString(html, "text/html")
    document.title = newDocument.title

    for (let i = 0; i < document.head.children.length; i += 1) {
        const documentHeadItem = document.head.children.item(i)

        if (!newDocument.head.innerHTML.includes(documentHeadItem.outerHTML)) {
            documentHeadItem.remove()
            i -= 1
        }
    }

    for (let i = 0; i < newDocument.head.children.length; i += 1) {
        const newDocumentHeadItem = newDocument.head.children.item(i)

        if (!document.head.innerHTML.includes(newDocumentHeadItem.outerHTML)) {
            document.head.appendChild(newDocumentHeadItem)
        }
    }

    document.body.replaceWith(newDocument.body)
}

const defaultOnPopState = window.onpopstate

/** @param {PopStateEvent} event */
function onPopState(event) {
    if (event.state && event.state.wave) {
       
        console.log('custom on pop state', event)

        replaceDocumentHtml(event.state.wave)
    }
}

function initOnPopState() {
    window.onpopstate = onPopState
    window.history.replaceState({ wave: document.documentElement.outerHTML }, '', window.location.href)
    console.log("init on pop state")
}

/** @param {string} url */
function pushCurrentDocumentToHistory(url) {
    window.history.pushState({ wave: document.documentElement.outerHTML }, '', url)
}

function ready(fn) {
    if (document.readyState === 'complete') {
        fn()
    } else {
        document.addEventListener('DOMContentLoaded', fn)
    }
}

ready(initOnPopState)
