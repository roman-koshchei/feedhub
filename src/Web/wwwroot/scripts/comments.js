if (!window.wave) {
    window.wave = []
}

console.log("loaded comments.js file")
document.addEventListener("DOMContentLoaded", () => {
    console.log("content loaded from js")
})



// TEMPORARY SOLUTION
// TODO: fix to not load scrips if already loaded?
// That's a difficult one I suppose
if (!window.wave.includes("comments.js")) {
    window.wave.push("comments.js")

const utf8decoder = new TextDecoder();



/** @param {Response} res */
async function readDocumentStream(res) {
    var reader = res.body.getReader()

    document.open() //unsafe
    //document.documentElement.innerHTML = ''
    //console.log(document.documentElement.innerHTML)
    //document.replaceChildren("")
    //console.log(document.documentElement)
    while (true) {
        const { done, value } = await reader.read();
        if (done) {
            document.close() //unsafe
            initOnPopState()
            break;
        }

        const html = utf8decoder.decode(value)
        //console.log(html)
        //document.documentElement.insertAdjacentHTML("beforeend", html)
        //document.documentElement.innerHTML += html
        document.write(html) // unsafe
    }
}

/**
 * @callback HandleHtmlCallback
 * @param {number} x
 * @returns {void}
 */

/** 
 * @param {Response} res 
 * @param {HandleHtmlCallback} handle
 */
async function readHtmlStream(res, handle) {
    handle(await res.text())
    //var reader = res.body.getReader()
    //while (true) {
    //    const { done, value } = await reader.read();
    //    if (done) {
    //        break
    //    }

    //    const html = utf8decoder.decode(value)
    //    handle(html)
    //}
}

/** @param {string} url */
function pushCurrentDocumentToHistory(url) {
    console.log({ wave: document.documentElement.outerHTML }, url)
    window.history.pushState({ wave: document.documentElement.outerHTML }, '', url)
}
function ready(fn) {
    if (document.readyState === 'complete') {
        fn()
    } else {
        document.addEventListener('DOMContentLoaded', fn)
    }
}

    //setTimeout(function testingTimout() {
    //    console.log("timeout")
    //    setTimeout(testingTimout, 1000)
    //}, 1000)

    

ready(function () {
    /** @type {HTMLFormElement} */
    const commentForm = document.getElementById("comment-form")
    const commentList = document.getElementById("comment-list")
    commentForm.addEventListener("submit", async function (event) {
        event.preventDefault()

        const res = await fetch(commentForm.action, {
            method: "POST",
            body: new FormData(commentForm),
            headers: { WaveJavascript: "" },
        })

        const redirect = res.headers.get("WaveRedirect")
        if (redirect) {
            window.location.href = redirect
            return
        }

        const html = await res.text()
        if (res.redirected) {
            replaceDocumentHtml(html)
            pushCurrentDocumentToHistory(res.url)
        } else if (res.ok) {
            commentList.insertAdjacentHTML("afterbegin", html)
        } else {
            replaceDocumentHtml(html)
            pushCurrentDocumentToHistory(res.url)
        }

        commentForm.reset()
    })
})
}