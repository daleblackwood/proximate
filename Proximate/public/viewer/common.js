"use strict";

const MS_SECOND = 1000;
const MS_MINUTE = MS_SECOND * 60;
const MS_HOUR = MS_MINUTE * 60;
const MS_DAY = MS_HOUR * 24;
const MS_WEEK = MS_DAY * 7;
const MS_YEARLIKE = MS_DAY * 365;
    
function showLogin(e) {
    if (e) console.error(e);
    setForm("Login", ["email:email", "password:password", "submit:submit"], data => {
        setBlocker();
        fetch("/auth", { headers: { 
            Authorization: `Basic ${btoa(`${data.email}:${data.password}`)}` }
        }).then(x => {
            if (x.status === 403) throw new Error("Unknown User")
            return x.text();
        }).then(x => {
            localStorage.setItem("lvt", x);
            load().catch(showLogin);
        }).catch(e => setError(e, "Login Error", showLogin));
    });
}

function setLabel(label, value) {
    value = String(value || "")
    const container = document.querySelector("main > menu");
    let elem = container.querySelector(`label[for=${label}]`);
    if (!elem) {            
        elem = dom("label", { for: label, innerText: label })
        container.appendChild(dom("div", null, [elem]));
    }
    let span = elem.querySelector("span") || elem.appendChild(dom("span"));
    const dateMatch = value?.match(/\d{4}-[01]\d-[0-3]\dT[0-2]\d:[0-5]\d:[0-5]\d/);
    if (dateMatch) {
        value = formatSince(value);
    }
    span.innerText = value;
    return elem;
}

function setForm(title, fieldNames, onSubmit) {
    const fields = fieldNames
        .map(x => x.split(":"))
        .map(([name, type = "text"]) => dom("input", {
            name,
            type
        }));
    const form = dom("form", {
        onsubmit: e => {
            e.preventDefault();
            e.stopImmediatePropagation();
            const data = {};
            for (const field of fields) {
                data[field.name] = field.value;
            }
            onSubmit(data);
            return false;
        }
    }, fields);
    setBlocker(form, title);
    return { form, fields };
}

function setError(error, title, onClick) {
    const btn = dom("button", {
        innerText: "OK",
        onclick: e => {
            e.preventDefault()
            onClick && onClick();
        }
    });
    setBlocker(btn, title || "Error", (error?.message ? error.message : error) || "Error")
}

function setBlocker(elem, title, description) {
    const id = "blocker";
    document.getElementById(id)?.remove();
    if (!elem) return;
    const blocker = dom("div", { id }, [
        dom("main", null, [
            dom("h3", { innerText: title }),
            dom("p", { innerText: description || "" }),
            elem
        ])
    ])
    document.body.append(blocker);
}

function api(url, msg) {
    return fetch(url, {
        headers: { "Authorization": "Bearer " + localStorage.getItem("lvt") },
        method: msg ? "POST" : "GET",
        body: msg ? JSON.stringify(msg) : undefined
    });
}

function formatDuration(seconds) {
    seconds = Math.floor(seconds);
    return formatTime(seconds / 3600, seconds / 60 % 60, seconds % 3600);
}
    
function formatTime(hours, minutes, seconds) {
    return (hours instanceof Date ? 
        [hours.getHours(), hours.getMinutes(), hours.getSeconds()] : 
        [hours, minutes, seconds]
    ).map((x, i) => i === 0 ? x > 0 ? x + "" : "" : String(Number(x)).padStart(2, "0"))
        .filter(Boolean)
        .join(":");
}

function formatDateTime(d) {
    if (typeof d === "string") d = new Date(d);
    if (!d || Number.isNaN(d.getTime())) return "-";
    return `${formatTime(d.getHours(), d.getMinutes(), d.getSeconds())} ${d.getDate()}/${d.getMonth() + 1}`;
}

function formatSince(d, decimals = 1) {
    let dt = (typeof d === "string" ? d = new Date(d) : d).valueOf() - Date.now();
    const p = dt >= 0 ? "+" : "-";
    dt = Math.abs(dt);
    if (dt > MS_YEARLIKE * 0.5) return p + (dt / MS_YEARLIKE).toFixed(decimals) + " years";
    if (dt > MS_WEEK * 2) return p + (dt / MS_WEEK).toFixed(decimals) + " weeks";
    if (dt > MS_DAY * 2) return p + (dt / MS_DAY).toFixed(decimals) + " days";
    if (dt > MS_HOUR * 2) return p + (dt / MS_HOUR).toFixed(decimals) + " hrs";
    if (dt > MS_MINUTE * 2) return p + (dt / MS_MINUTE).toFixed(decimals) + " min";
    return p + (dt / MS_SECOND).toFixed(decimals) + " sec";
}

function formatLocalIsoDate(date) {
    return date.toLocaleString("sv-SE", { 
        year: "numeric", 
        month: "2-digit", 
        day: "2-digit", 
        hour: "2-digit", 
        minute: "2-digit", 
        second: undefined,
        hour12: false
    }).replace(" ", "T");
}

function dom(tagOrEl, props, children) {
    const el = tagOrEl instanceof HTMLElement ? tagOrEl : document.createElement(tagOrEl);
    props && Object.entries(props).forEach(([k, v]) => 
        k === "style" ? Object.assign(el.style, v) : k in el ? el[k] = v : el.setAttribute(k, v));
    (Array.isArray(children) ? children : [children]).forEach(x => x && el.appendChild(x));
    return el;
}

function cls(elem, className, on) {
    on ? elem.classList.add(className) : elem.classList.remove(className);
}